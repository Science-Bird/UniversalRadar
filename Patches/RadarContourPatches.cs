using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.AI;
using System.Text.RegularExpressions;
using TerraMesh.Utils;
using TerraMesh;
using EasyTextEffects.Editor.MyBoxCopy.Extensions;
using System.Reflection;

namespace UniversalRadar.Patches
{
    [HarmonyPatch]
    public class RadarContourPatches
    {
        public static Material contourMaterial;
        public static List<GameObject> terrainObjects = new List<GameObject>();
        public static List<GameObject> unityMeshTerrains = new List<GameObject>();
        public static Dictionary<(string,string), MaterialProperties> contourDataDict = new Dictionary<(string, string), MaterialProperties>();
        public static Dictionary<(string, string), List<string>> terrainMemoryDict = new Dictionary<(string, string), List<string>>();
        public static Dictionary<(string, string), List<MeshTerrainInfo>> meshTerrainDict = new Dictionary<(string, string), List<MeshTerrainInfo>>();
        public static float terrainMax;
        public static float terrainMin;
        private static float stroke = UniversalRadar.AutoLineWidth.Value;
        private static float lineSpace = UniversalRadar.AutoSpacing.Value;
        private static float maxOpacity = UniversalRadar.AutoOpacity.Value;
        private static float opacityMultiplier = UniversalRadar.AutoMultiplier.Value;
        public static readonly Vector4 defaultGreen = new Vector4(0.3019608f, 0.4156863f, 0.2745098f, 1f);
        public static readonly Vector3 verticalOffset = new Vector3(0f, 0.05f, 0f);
        public static bool loaded = false;

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SceneManager_OnLoadComplete1))]
        [HarmonyPrefix]
        static void OnLoadLevel(StartOfRound __instance, string sceneName)
        {
            if (!loaded && sceneName == __instance.currentLevel.sceneName)// on loading a moon
            {
                loaded = true;// make sure it doesn't run twice
                (string, string) moonIdentifier = (__instance.currentLevel.PlanetName, __instance.currentLevel.sceneName);// I use this as a convenient way of specifically identifying moons with without an API (display name + internal name), since using LLL here would mean a hard dependency or restructuring

                UniversalRadar.Logger.LogDebug($"LOADING LEVEL: {__instance.currentLevel.PlanetName}");
                if (ConfigPatch.moonBlacklist.Contains(moonIdentifier))
                {
                    return;
                }

                ExtraRadarPatches.AddNewRadarSprites(__instance.currentLevel.sceneName);
                //ExtraRadarPatches.ChangeWater();

                if (contourMaterial == null)
                {
                    UniversalRadar.Logger.LogDebug("Loading contour material!");
                    contourMaterial = (Material)UniversalRadar.URAssets.LoadAsset("ContourMat");
                }
                //if (radarWaterMaterial == null)
                //{
                //    radarWaterMaterial = (Material)UniversalRadar.URAssets.LoadAsset("WaterLines");
                //    radarWaterMaterial.renderQueue = 1000;
                //}


                if (contourDataDict.TryGetValue(moonIdentifier, out MaterialProperties value) && !value.extendHeight)// if moon terrain parameters have been computed before
                {
                    UniversalRadar.Logger.LogDebug($"Found level in dictionary: {__instance.currentLevel.PlanetName}");
                    FetchTerrainObjects(true, moonIdentifier);
                    if (terrainObjects.Count == 0 && unityMeshTerrains.Count == 0)
                    {
                        return;
                    }
                    value.LogAllProperties();
                    value.SetProperties(contourMaterial);// updates contour shader with stored values
                    SetupContourMeshes();// create and assign materials to meshes
                }
                else
                {
                    bool fullHeight = false;
                    if (value != null && value.extendHeight)
                    {
                        fullHeight = true;
                    }
                    FetchTerrainObjects(false, moonIdentifier);
                    if (terrainObjects.Count == 0 && unityMeshTerrains.Count == 0)
                    {
                        return;
                    }
                    // default property generation for max and min: halve whatever the terrain max/min are (since terrain generally exceeds the normal playable area by a bit), if terrain max is exceptionally large, quarter it, and ensure terrain min is at most zero (which is the vertical level of ship landing spot)
                    float minHeight = terrainMin < 0 ? (terrainMin / 2) : 0;
                    float maxHeight = fullHeight ? terrainMax : terrainMax > 100 ? (terrainMax / 3) : (terrainMax / 2);
                    MaterialProperties newProperties = new MaterialProperties(lineSpace, stroke, minHeight, maxHeight, maxOpacity, opacityMultiplier, defaultGreen, defaultGreen);
                    newProperties.LogAllProperties();
                    if (contourDataDict.ContainsKey(moonIdentifier))
                    {
                        contourDataDict.Remove(moonIdentifier);
                    }
                    contourDataDict.Add(moonIdentifier, newProperties);// remember properties for next time
                    newProperties.SetProperties(contourMaterial);// updates contour shader with calculated values
                    SetupContourMeshes();// create and assign materials to meshes
                }
                GameObject contourObj = GameObject.FindGameObjectWithTag("TerrainContourMap");
                if (contourObj != null && (bool)contourObj.GetComponent<SpriteRenderer>())
                {
                    UniversalRadar.Logger.LogDebug("Disabling existing contour map!");
                    contourObj.GetComponent<SpriteRenderer>().enabled = false;
                }
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ReviveDeadPlayers))]
        [HarmonyPostfix]
        public static void ResetLoadedFlag()
        {
            loaded = false;
        }

        public static void FetchTerrainObjects(bool hasMatInfo, (string,string) identifier)
        {
            terrainObjects.Clear();
            unityMeshTerrains.Clear();
            List<Terrain> unityTerrains = Object.FindObjectsOfType<Terrain>().Where(x => x.gameObject.activeInHierarchy && x.enabled && x.drawHeightmap && x.GetComponent<TerrainCollider>() && x.GetComponent<TerrainCollider>().enabled).ToList();

            Bounds exteriorNavMeshBounds = new Bounds(Vector3.zero, Vector3.zero);
            bool usingNavMesh = false;
            if (!hasMatInfo || (!terrainMemoryDict.ContainsKey(identifier) && (!meshTerrainDict.ContainsKey(identifier) && unityTerrains.Count > 0)))// if doesn't have material info or no previously generated terrain can be found (nav mesh is used both for min/max height and to find terrain objects)
            {
                List<NavMeshSurface> navSurfaces = Object.FindObjectsOfType<NavMeshSurface>().ToList();
                if (navSurfaces.Count > 0)// calculate playable area of level using nav mesh (i.e. where AI can navigate and stuff can spawn)
                {
                    usingNavMesh = true;
                    UniversalRadar.Logger.LogDebug($"Found nav meshes!");
                    List<NavMeshSurface> navEnvironment = navSurfaces.Where(x => x.gameObject.name == "Environment").ToList();
                    GameObject navMeshRoot = navEnvironment.Count > 0 ? navEnvironment.First().gameObject : navSurfaces.First().gameObject;// if there are multiple nav mesh surfaces (not even sure if that's possible lol), use the one of the "Environment" object (standard in vanilla)
                    foreach (Vector3 vert in NavMesh.CalculateTriangulation().vertices)// since this is on initial scene load, interior dungeon hasnt generated yet, so only nav mesh present is exterior
                    {
                        exteriorNavMeshBounds.Encapsulate(vert);// calculate bounds of nav mesh by encapsulating all vertices (could maybe make more efficient idk)
                    }
                    UniversalRadar.Logger.LogDebug($"NAV MAX: {exteriorNavMeshBounds.max.y}, NAV MIN: {exteriorNavMeshBounds.min.y}");
                }
            }

            if (terrainMemoryDict.TryGetValue(identifier, out List<string> values))// if game object paths are stored from previous generation
            {
                for (int i = 0; i < values.Count; i++)
                {
                    GameObject terrainObj = GameObject.Find(values[i]);
                    if (terrainObj != null && !terrainObjects.Contains(terrainObj))
                    {
                        terrainObjects.Add(terrainObj);
                    }
                }
            }
            else
            {
                // find mesh colliders with renderers that are: named terrain OR are very large in size, AND it and its parents are not labelled "out of bounds" AND its mesh is not labelled "cube"
                MeshCollider[] terrainColliders = Object.FindObjectsOfType<MeshCollider>().Where(x => (x.gameObject.name.Contains("Terrain", System.StringComparison.Ordinal) || x.bounds.size.magnitude > 400f) && !GetObjectPath(x.gameObject).Contains("OutOfBounds", System.StringComparison.Ordinal) && !x.sharedMesh.name.Contains("Cube", System.StringComparison.Ordinal) && (bool)x.gameObject.GetComponent<MeshRenderer>()).ToArray();
                // find mesh colliders with PARENTS that are renderers, both the child collider and parent renderer must meet the same conditions as stated above
                MeshCollider[] terrainColliderChildren = Object.FindObjectsOfType<MeshCollider>().Where(x => x.transform.parent != null && (bool)x.transform.parent.gameObject.GetComponent<MeshRenderer>() && (x.transform.parent.gameObject.name.Contains("Terrain", System.StringComparison.Ordinal) || x.bounds.size.magnitude > 400f || x.transform.parent.gameObject.GetComponent<MeshRenderer>().bounds.size.magnitude > 400f) && !GetObjectPath(x.transform.parent.gameObject).Contains("OutOfBounds", System.StringComparison.Ordinal) && !x.sharedMesh.name.Contains("Cube", System.StringComparison.Ordinal)).ToArray();

                if (terrainColliders.Length == 0 && terrainColliderChildren.Length == 0 && unityTerrains.Count == 0)
                {
                    UniversalRadar.Logger.LogWarning("Unable to find any terrain objects on this moon!");
                    return;
                }
                terrainMax = -10000;
                terrainMin = 10000;

                List<string> paths = new List<string>();
                for (int i = 0; i < terrainColliders.Length; i++)
                {
                    MeshRenderer terrainRenderer = terrainColliders[i].gameObject.GetComponent<MeshRenderer>();
                    UniversalRadar.Logger.LogDebug($"Collider bounds ({terrainColliders[i].gameObject.name}): {terrainRenderer.bounds.size.x}, {terrainRenderer.bounds.size.y}, {terrainRenderer.bounds.size.z} - {terrainRenderer.bounds.size.magnitude}");
                    if (!terrainObjects.Contains(terrainColliders[i].gameObject) && ((usingNavMesh && WithinNavMesh(exteriorNavMeshBounds, terrainRenderer.bounds)) || !usingNavMesh))// not already added and suitably close to nav mesh
                    {
                        terrainObjects.Add(terrainColliders[i].gameObject);
                        paths.Add(GetObjectPath(terrainColliders[i].gameObject));
                    }
                    terrainMax = Mathf.Max(terrainRenderer.bounds.max.y, terrainMax);
                    terrainMin = Mathf.Min(terrainRenderer.bounds.min.y, terrainMin);
                }
                for (int i = 0; i < terrainColliderChildren.Length; i++)
                {
                    MeshRenderer terrainRenderer = terrainColliderChildren[i].transform.parent.gameObject.GetComponent<MeshRenderer>();
                    if (!terrainObjects.Contains(terrainColliderChildren[i].transform.parent.gameObject) && ((usingNavMesh && WithinNavMesh(exteriorNavMeshBounds, terrainRenderer.bounds)) || !usingNavMesh))// not already added and suitably close to nav mesh
                    {
                        terrainObjects.Add(terrainColliderChildren[i].transform.parent.gameObject);
                        paths.Add(GetObjectPath(terrainColliderChildren[i].transform.parent.gameObject));
                    }
                    terrainMax = Mathf.Max(terrainRenderer.bounds.max.y, terrainMax);
                    terrainMin = Mathf.Min(terrainRenderer.bounds.min.y, terrainMin);
                }
                terrainMemoryDict.Add(identifier, paths);// add terrain object paths for next cycle
            }


            if (unityTerrains.Count > 0)// unity terrain objects need to be converted to actual meshes via TerraMesh, requires separate handling
            {
                if (meshTerrainDict.TryGetValue(identifier, out List<MeshTerrainInfo> meshes))// since the objects dont actually exist yet to find, previously made mesh terrain info is stored in a custom struct
                {
                    for (int i = 0; i < meshes.Count; i++)
                    {
                        GameObject meshTerrain = new GameObject($"NewMeshTerrain{i}");
                        if (!meshes[i].parentPath.IsNullOrEmpty())
                        {
                            GameObject parentObj = GameObject.Find(meshes[i].parentPath);
                            if (parentObj != null)
                            {
                                meshTerrain.transform.SetParent(parentObj.transform);
                            }
                        }
                        MeshFilter terrainMeshFilter = meshTerrain.AddComponent<MeshFilter>();
                        terrainMeshFilter.sharedMesh = meshes[i].mesh;
                        MeshRenderer terrainMeshRenderer = meshTerrain.AddComponent<MeshRenderer>();
                        terrainMeshRenderer.material = contourMaterial;
                        meshTerrain.transform.localPosition = meshes[i].position;
                        meshTerrain.transform.localRotation = meshes[i].rotation;
                        meshTerrain.transform.localScale = meshes[i].scale;
                        unityMeshTerrains.Add(meshTerrain);
                    }
                }
                else
                {
                    MeshifyTerrains(unityTerrains, !hasMatInfo, identifier, exteriorNavMeshBounds);// create new mesh terrain
                }
            }
            if (usingNavMesh)// if nav mesh max/min are stricter than calculated from terrain, use them instead (if nav mesh extends higher/lower than terrain does, usually means there's some silly nav meshed spot well outside terrain like roof of tall building, so we ignore it)
            {
                terrainMax = Mathf.Min(exteriorNavMeshBounds.max.y, terrainMax);
                terrainMin = Mathf.Max(exteriorNavMeshBounds.min.y, terrainMin);
            }
            UniversalRadar.Logger.LogDebug($"Terrain max: {terrainMax}, terrain min: {terrainMin}");
        }

        public static void MeshifyTerrains(List<Terrain> terrains, bool findBounds, (string, string) identifier, Bounds navMeshBounds)// use TerraMesh to convert Unity terrain objects into actual meshes we can use
        {
            Shader tmShader = Shader.Find("HDRP/Lit");// random generic shader since we're immediately replacing terrain material anyways
            TerraMeshConfig tmConfig = new TerraMeshConfig(detailInstancingBatchSize: 1023, terraMeshShader: tmShader);
            List<MeshTerrainInfo> terrainInfos = new List<MeshTerrainInfo>();
            for (int i = 0; i < terrains.Count; i++)
            {
                if (!WithinNavMesh(navMeshBounds, terrains[i].GetComponent<TerrainCollider>().bounds))
                {
                    continue;
                }

                UniversalRadar.Logger.LogDebug($"--Unity terrain object at: {GetObjectPath(terrains[i].gameObject)}");
                GameObject meshTerrain = terrains[i].Meshify(tmConfig);// this takes a little bit (up to a couple seconds depending, idk)
                if (meshTerrain != null)
                {
                    MeshRenderer renderer = meshTerrain.GetComponent<MeshRenderer>();
                    if (renderer != null && !unityMeshTerrains.Contains(meshTerrain))
                    {
                        if (findBounds)
                        {
                            terrainMax = Mathf.Max(renderer.bounds.max.y, terrainMax);
                            terrainMin = Mathf.Min(renderer.bounds.min.y, terrainMin);
                        }
                        unityMeshTerrains.Add(meshTerrain);
                        terrainInfos.Add(new MeshTerrainInfo(meshTerrain));// save info about game object to recreate later without re-meshifying terrain
                    }
                    else
                    {
                        meshTerrain.SetActive(false);
                    }
                }
                // TerraMesh automatically disables existing terrain, so we re-enable it since we don't want to change the map's terrain
                terrains[i].gameObject.SetActive(true);
                terrains[i].enabled = true;
                terrains[i].drawHeightmap = true;
                terrains[i].GetComponent<TerrainCollider>().enabled = true;
            }
            meshTerrainDict.Add(identifier, terrainInfos);// save gathered info so we don't have to re-generate meshes next time
        }

        public static bool WithinNavMesh(Bounds navBounds, Bounds terrainBounds)
        {
            Vector3 center = new Vector3(terrainBounds.center.x, navBounds.center.y, terrainBounds.center.z);
            UniversalRadar.Logger.LogDebug($"terrain: {terrainBounds.center}; nav: {navBounds.center}");
            if (!navBounds.Intersects(terrainBounds))// center of terrain obj is outside of nav region (stricter version of intersection)
            {
                UniversalRadar.Logger.LogDebug("EXCLUDING: Center of terrain not within nav mesh bounds!");
                return false;
            }
            UniversalRadar.Logger.LogDebug($"terrain: {terrainBounds.min.x}, {terrainBounds.max.x}; nav: {navBounds.min.x}, {navBounds.max.x}");
            if ((terrainBounds.max.x > navBounds.max.x || terrainBounds.min.x < navBounds.min.x) && terrainBounds.size.x - navBounds.size.x > navBounds.size.x * 8)// terrain x-bounds >800% bigger than nav region
            {
                UniversalRadar.Logger.LogDebug("EXCLUDING: x-dimension of terrain significantly exceeds nav mesh bounds!");
                return false;
            }
            UniversalRadar.Logger.LogDebug($"terrain: {terrainBounds.min.z}, {terrainBounds.max.z}; nav: {navBounds.min.z}, {navBounds.max.z}");
            if ((terrainBounds.max.z > navBounds.max.z || terrainBounds.min.z < navBounds.min.z) && terrainBounds.size.z - navBounds.size.z > navBounds.size.z * 8)// terrain z-bounds >800% bigger than nav region
            {
                UniversalRadar.Logger.LogDebug("EXCLUDING: z-dimension of terrain significantly exceeds nav mesh bounds!");
                return false;
            }
            return true;
        }

        public static void SetupContourMeshes()
        {
            for (int i = 0; i < terrainObjects.Count; i++)// instantiate new objects and set up their parameters for contour shader material
            {
                UniversalRadar.Logger.LogDebug($"Terrain object at: {GetObjectPath(terrainObjects[i])}");
                GameObject contourTerrain = Object.Instantiate(terrainObjects[i], terrainObjects[i].transform.position, terrainObjects[i].transform.rotation, terrainObjects[i].transform.parent);
                contourTerrain.name = terrainObjects[i].name + "_ContourMesh";
                contourTerrain.layer = 14;
                Collider[] colliders = contourTerrain.GetComponentsInChildren<Collider>();
                for (int j = 0; j < colliders.Length; j++)// maybe destroy all non-renderer components
                {
                    colliders[j].enabled = false;
                }
                Material[] materials = contourTerrain.GetComponent<MeshRenderer>().materials;
                for (int j = 0; j < materials.Length; j++)
                {
                    materials[j] = contourMaterial;
                }
                contourTerrain.transform.position += verticalOffset;
                contourTerrain.GetComponent<MeshRenderer>().materials = materials;
            }
            for (int i = 0; i < unityMeshTerrains.Count; i++)// Find created mesh terrains and set up their parameters for contour shader material
            {
                UniversalRadar.Logger.LogDebug($"Unity terrain object at: {GetObjectPath(unityMeshTerrains[i])}");
                unityMeshTerrains[i].name = unityMeshTerrains[i].name + "_ContourMesh";
                unityMeshTerrains[i].layer = 14;
                Collider[] colliders = unityMeshTerrains[i].GetComponentsInChildren<Collider>();
                for (int j = 0; j < colliders.Length; j++)
                {
                    colliders[j].enabled = false;
                }
                Material[] materials = unityMeshTerrains[i].GetComponent<MeshRenderer>().materials;
                for (int j = 0; j < materials.Length; j++)
                {
                    materials[j] = contourMaterial;
                }
                unityMeshTerrains[i].transform.position += verticalOffset;// to avoid weird z-fighting bug
                unityMeshTerrains[i].GetComponent<MeshRenderer>().materials = materials;
            }
        }

        public static string GetObjectPath(GameObject obj)
        {
            StringBuilder path = new StringBuilder(obj.name);
            Transform current = obj.transform.parent;

            while (current != null)
            {
                path.Insert(0, current.name + "/");
                current = current.parent;
            }

            return path.ToString();
        }
    }


    [HarmonyPatch]
    public class ExtraRadarPatches
    {
        [HarmonyPatch(typeof(ManualCameraRenderer), nameof(ManualCameraRenderer.MapCameraFocusOnPosition))]
        [HarmonyPostfix]
        static void CameraPatch(ManualCameraRenderer __instance)// clipping plane is normally so tight that only a narrow vertical band around player is captured, so it needs to be extended to capture the terrain's contour map
        {
            if (!(GameNetworkManager.Instance.localPlayerController == null))
            {
                __instance.mapCamera.nearClipPlane -= UniversalRadar.CameraClipExtension.Value;
                __instance.mapCamera.farClipPlane += UniversalRadar.CameraClipExtension.Value;
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SwitchMapMonitorPurpose))]
        [HarmonyPostfix]
        static void OnRadarEnable(StartOfRound __instance, bool displayInfo)
        {
            if (!displayInfo)
            {
                FieldInfo field = AccessTools.Field(typeof(ManualCameraRenderer), "checkedForContourMap");
                if (field != null)
                {
                    field.SetValue(__instance.mapScreen, false);
                }
            }
        }

        //public static void ChangeWater()
        //{
        //    MeshRenderer[] waterRenderers = Object.FindObjectsOfType<MeshRenderer>().Where(x => x.material != null && (x.material.shader.name.ToLower().Contains("water") || x.material.name.ToLower().Contains("water"))).ToArray();
        //    for (int i = 0; i < waterRenderers.Length; i++)
        //    {
        //        UniversalRadar.Logger.LogDebug($"SHADER: {waterRenderers[i].material.shader.name}, MAT: {waterRenderers[i].material.name}");
        //        GameObject radarWater = new GameObject("WaterRadarLines");
        //        radarWater.layer = 14;
        //        radarWater.transform.SetParent(waterRenderers[i].gameObject.transform);
        //        radarWater.transform.localPosition = Vector3.zero;
        //        radarWater.transform.localRotation = Quaternion.identity;
        //        radarWater.transform.localScale = Vector3.one;
        //        MeshFilter filter = radarWater.AddComponent<MeshFilter>();
        //        filter.sharedMesh = waterRenderers[i].gameObject.GetComponent<MeshFilter>().sharedMesh;
        //        MeshRenderer renderer = radarWater.AddComponent<MeshRenderer>();
        //        Material[] materials = new Material[waterRenderers[i].materials.Length];
        //        for (int j = 0; j < materials.Length; j++)
        //        {
        //            materials[j] = RadarContourPatches.radarWaterMaterial;
        //        }
        //        renderer.materials = materials;
        //    }
        //}

        public static void AddNewRadarSprites(string sceneName)// disable existing map radar objects and replace them with my custom-made ones (for vanilla)
        {
            if (sceneName == "Level4March" || sceneName == "Level8Titan")
            {
                GameObject contourMap = new GameObject("ContourMap");
                contourMap.transform.SetParent(GameObject.Find("Environment").transform);
                GameObject newContourObj = new GameObject("ContourMapTerrain");
                newContourObj.transform.SetParent(contourMap.transform);
                contourMap.transform.localPosition = Vector3.zero;
                contourMap.transform.localRotation = Quaternion.identity;
                contourMap.transform.localScale = Vector3.one;
                newContourObj.transform.localPosition = new Vector3(0f, 77.7f, 0f);
                newContourObj.transform.localRotation = Quaternion.identity;
                newContourObj.transform.localScale = Vector3.one;
                newContourObj.tag = "TerrainContourMap";
                newContourObj.layer = 14;
            }

            GameObject contourObj = GameObject.FindGameObjectWithTag("TerrainContourMap");
            if (contourObj != null)
            {
                if (UniversalRadar.radarSpritePrefabs.TryGetValue(sceneName, out GameObject value))
                {
                    SpriteRenderer[] existingSprites = contourObj.GetComponentsInChildren<SpriteRenderer>();
                    for (int i = 0; i < existingSprites.Length; i++)
                    {
                        existingSprites[i].enabled = false;
                    }
                    GameObject newSprites = Object.Instantiate(value, contourObj.transform);
                    newSprites.transform.localPosition = Vector3.zero;
                    newSprites.transform.localRotation = Quaternion.identity;
                    newSprites.transform.localScale = Vector3.one;
                    if (!UniversalRadar.WaterSprites.Value)
                    {
                        Transform water = newSprites.transform.Find("Water");
                        if (water != null)
                        {
                            water.gameObject.GetComponent<SpriteRenderer>().enabled = false;
                        }
                    }
                }
            }
        }
    }

    public class MeshTerrainInfo
    {
        public Mesh mesh;
        public string parentPath;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;

        public MeshTerrainInfo(GameObject gameObject)
        {
            mesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
            parentPath = "";
            if (gameObject.transform.parent != null)
            {
                parentPath = RadarContourPatches.GetObjectPath(gameObject.transform.parent.gameObject);
            }
            position = gameObject.transform.localPosition;
            rotation = gameObject.transform.localRotation;
            scale = gameObject.transform.localScale;
        }
    }


    public class MaterialProperties
    {
        public bool extendHeight;
        public float lineSpacing;
        public float lineThickness;
        public float minHeight;
        public float maxHeight;
        public float opacityCap;
        public float opacityMult;
        public Vector4 baseColour;
        public Vector4 lineColour;

        public MaterialProperties(float spacing, float thickness, float min, float max, float opacity, float multiplier, Vector4 colourBG, Vector4 colourLine)
        {
            extendHeight = false;
            lineSpacing = spacing;
            lineThickness = thickness;
            minHeight = min;
            maxHeight = max;
            opacityCap = opacity;
            opacityMult = multiplier;
            baseColour = colourBG;
            lineColour = colourLine;
            if (min > max)
            {
                minHeight = max;
                maxHeight = min;
            }
        }

        public MaterialProperties(MaterialPropertiesConfig propertiesConfig)// generate material properties from config values (when values are to be set manually)
        {
            if (propertiesConfig.mode.Value == "Auto")
            {
                extendHeight = propertiesConfig.extendHeight.Value;
            }
            else
            {
                extendHeight = false;
                lineSpacing = propertiesConfig.lineSpacing.Value;
                lineThickness = propertiesConfig.lineThickness.Value;
                minHeight = propertiesConfig.minHeight.Value;
                maxHeight = propertiesConfig.maxHeight.Value;
                opacityCap = propertiesConfig.opacityCap.Value;
                opacityMult = propertiesConfig.opacityMult.Value;

                Vector4 colourBG = ColourFromHex(propertiesConfig.baseColourHex.Value);
                baseColour = RadarContourPatches.defaultGreen;
                if (colourBG.x >= 0)
                {
                    baseColour = colourBG;
                }
                Vector4 colourLine = ColourFromHex(propertiesConfig.lineColourHex.Value);
                lineColour = RadarContourPatches.defaultGreen;
                if (colourLine.x >= 0)
                {
                    lineColour = colourLine;
                }

                if (propertiesConfig.minHeight.Value > propertiesConfig.maxHeight.Value)
                {
                    minHeight = propertiesConfig.maxHeight.Value;
                    maxHeight = propertiesConfig.minHeight.Value;
                }
            }
        }

        public void LogAllProperties()
        {
            UniversalRadar.Logger.LogDebug($"SPACING: {lineSpacing}");
            UniversalRadar.Logger.LogDebug($"THICKNESS: {lineThickness}");
            UniversalRadar.Logger.LogDebug($"MIN: {minHeight}");
            UniversalRadar.Logger.LogDebug($"MAX: {maxHeight}");
            UniversalRadar.Logger.LogDebug($"MAX OPACITY: {opacityCap}");
            UniversalRadar.Logger.LogDebug($"OPACITY MULT: {opacityMult}");
        }

        public Vector4 ColourFromHex(string hexCode)
        {
            string hex = hexCode.Replace("#","").ToUpper();
            if (hex.Length == 6 && Regex.Match(hex, "^#[A-F0-9]{6}$").Success)
            {
                float R = System.Convert.ToInt32(hex.Substring(0, 2), 16) / 255f;
                float G = System.Convert.ToInt32(hex.Substring(2, 2), 16) / 255f;
                float B = System.Convert.ToInt32(hex.Substring(4, 2), 16) / 255f;
                return new Vector4(R, G, B, 1f);
            }
            return new Vector4(-1f, -1f, -1f, -1f);
        }

        public void SetProperties(Material contourMat)// set material properties onto the actual material
        {
            contourMat.renderQueue = 1000;
            contourMat.SetFloat("_ContourSpacing", lineSpacing);
            contourMat.SetFloat("_LineWidth", lineThickness);
            contourMat.SetFloat("_HeightMin", minHeight);
            contourMat.SetFloat("_HeightMax", maxHeight);
            contourMat.SetFloat("_MaxOpacity", opacityCap);
            contourMat.SetFloat("_OpacityMultiplier", opacityMult);
        }
    }
}
