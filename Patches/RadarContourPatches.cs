using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using UnityEngine;
using UnityEngine.AI;
using System.Text.RegularExpressions;
using TerraMesh.Utils;
using TerraMesh;
using EasyTextEffects.Editor.MyBoxCopy.Extensions;
using UnityEngine.Rendering.HighDefinition;
using Unity.Netcode;

namespace UniversalRadar.Patches
{
    [HarmonyPatch]
    public class RadarContourPatches
    {
        public static Material contourMaterial = UniversalRadar.contourMaterial;
        public static Material radarFillMat0 = UniversalRadar.radarFillMat0;
        public static Material radarFillMat1 = UniversalRadar.radarFillMat1;
        public static Material radarWaterMat = UniversalRadar.radarWaterMat;
        public static List<GameObject> terrainObjects = new List<GameObject>();
        public static List<GameObject> mapGeometry = new List<GameObject>();
        public static List<GameObject> unityMeshTerrains = new List<GameObject>();
        public static List<GameObject> waterObjects = new List<GameObject>();
        public static Dictionary<(string,string), MaterialProperties> contourDataDict = new Dictionary<(string, string), MaterialProperties>();
        public static Dictionary<(string, string), List<string>> terrainMemoryDict = new Dictionary<(string, string), List<string>>();
        public static Dictionary<(string, string), List<string>> geometryMemoryDict = new Dictionary<(string, string), List<string>>();
        public static Dictionary<(string, string), List<MeshTerrainInfo>> meshTerrainDict = new Dictionary<(string, string), List<MeshTerrainInfo>>();
        public static float terrainMax;
        public static float terrainMin;
        private static float stroke = UniversalRadar.AutoLineWidth.Value;
        private static float lineSpace = UniversalRadar.AutoSpacing.Value;
        private static float maxOpacity = UniversalRadar.AutoOpacity.Value;
        public static readonly Vector4 defaultGreen = new Vector4(0.3019608f, 0.4156863f, 0.2745098f, 1f);
        public static readonly Vector4 radarFillGreen = new Vector4(0.3882353f, 1, 0.3529412f, 1f);
        public static readonly Vector3 verticalOffset = new Vector3(0f, 0.05f, 0f);
        public static readonly Vector3 shipPos = new Vector3(3f, 0f, -15f);
        public static bool loaded = false;
        private static readonly HashSet<System.Type> keepTypes = new HashSet<System.Type> { typeof(Transform), typeof(MeshFilter), typeof(MeshRenderer) };
        private static readonly HashSet<System.Type> blacklistTypes = new HashSet<System.Type> { typeof(NetworkObject), typeof(Animator), typeof(SkinnedMeshRenderer) };
        private static readonly HashSet<System.Type> disableTypes = new HashSet<System.Type> { typeof(AudioSource), typeof(Light), typeof(HDAdditionalLightData)};
        private static readonly bool showFoliage = UniversalRadar.ShowFoliage.Value;
        public static bool disableMoon = false;
        //private static readonly string[] vanillaMoons = ["20 Adamance", "68 Artifice", "220 Assurance", "71 Gordion", "7 Dine", "5 Embrion", "41 Experimentation", "44 Liquidation", "61 March", "21 Offense", "85 Rend", "8 Titan", "56 Vow"];
        public static bool fullHeight;

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SceneManager_OnLoadComplete1))]
        [HarmonyPrefix]
        static void OnLoadLevel(StartOfRound __instance, string sceneName)
        {
            if (!loaded && sceneName == __instance.currentLevel.sceneName)// on loading a moon
            {
                disableMoon = false;
                //GameObject uiObj = GameObject.Find("ItemSystems/MapScreenUI");
                //if (uiObj != null)
                //{
                //    uiObj.SetActive(false);
                //}
                //GameObject camObj = GameObject.Find("Systems/HeadMountedCamera");
                //if (camObj != null)
                //{
                //    camObj.SetActive(false);
                //}
                (string, string) moonIdentifier = (__instance.currentLevel.PlanetName, sceneName);// I use this as a convenient way of specifically identifying moons with without an API (display name + internal name), since using LLL here would mean a hard dependency or restructuring
                disableMoon = ConfigPatch.moonBlacklist.Contains(moonIdentifier);
                ClearRadarAddWater(disableMoon);// clear any radar objects which got carried over from a previous moon (also adds water radar objects if the moon isn't disabled)
                loaded = true;// make sure this patch doesn't run twice
                

                if (disableMoon) { return; }

                RadarExtraPatches.AddNewRadarSprites(moonIdentifier);

                if (contourMaterial == null || radarFillMat0 == null || radarFillMat1 == null || radarWaterMat == null)
                {
                    contourMaterial = (Material)UniversalRadar.URAssets.LoadAsset("ContourMat");
                    radarFillMat0 = (Material)UniversalRadar.URAssets.LoadAsset("RadarGreen0");// regular
                    radarFillMat1 = (Material)UniversalRadar.URAssets.LoadAsset("RadarGreen1");// low opacity
                    radarWaterMat = (Material)UniversalRadar.URAssets.LoadAsset("RadarBlue");// water
                    radarWaterMat.renderQueue = 1000;
                }

                Bounds exteriorNavMeshBounds = new Bounds(Vector3.zero, Vector3.zero);

                if (!terrainMemoryDict.ContainsKey(moonIdentifier) && !meshTerrainDict.ContainsKey(moonIdentifier))// if no previously generated terrain can be found (nav mesh is used both for min/max height and to find terrain objects)
                {
                    foreach (Vector3 vert in NavMesh.CalculateTriangulation().vertices)// since this is on initial scene load, interior dungeon hasnt generated yet, so only nav mesh present is exterior
                    {
                        exteriorNavMeshBounds.Encapsulate(vert);// calculate bounds of nav mesh by encapsulating all vertices (could maybe make more efficient idk)
                    }
                    UniversalRadar.Logger.LogDebug($"NAV MAX: {exteriorNavMeshBounds.max.y}, NAV MIN: {exteriorNavMeshBounds.min.y}");
                    if (exteriorNavMeshBounds.size == Vector3.zero)
                    {
                        UniversalRadar.Logger.LogError("Unable to find nav mesh!");
                        return;
                    }
                }

                if (contourDataDict.TryGetValue(moonIdentifier, out MaterialProperties value) && !value.auto)// if moon terrain parameters have been computed before
                {
                    FetchTerrainObjects(hasMatInfo: true, moonIdentifier, exteriorNavMeshBounds);
                    if (value.showObjects)
                    {
                        FetchMapGeometry(moonIdentifier, exteriorNavMeshBounds);
                    }
                    else
                    {
                        mapGeometry.Clear();
                    }
                    if (terrainObjects.Count == 0 && unityMeshTerrains.Count == 0 && mapGeometry.Count == 0 && waterObjects.Count == 0)
                    {
                        return;
                    }
                    value.LogAllProperties();
                    value.SetProperties(contourMaterial);// updates contour shader with stored values
                    SetupMeshes(value.lowObjectOpacity);// create and assign materials to meshes
                }
                else
                {
                    bool showGeometry = true;
                    bool subtleObjects = false;
                    fullHeight = false;
                    float multiplier = 2f;
                    Vector4 color = defaultGreen;
                    if (value != null && value.auto)
                    {
                        showGeometry = value.showObjects;
                        subtleObjects = value.lowObjectOpacity;
                        fullHeight = value.extendHeight;
                        multiplier = value.opacityMult;
                        color = value.baseColour;
                    }
                    if (UniversalRadar.HideRadarObjects.Value)
                    {
                        showGeometry = false;
                    }
                    FetchTerrainObjects(hasMatInfo: false, moonIdentifier, exteriorNavMeshBounds);
                    if (showGeometry)
                    {
                        FetchMapGeometry(moonIdentifier, exteriorNavMeshBounds);
                    }
                    else
                    {
                        mapGeometry.Clear();
                    }
                    if (terrainObjects.Count == 0 && unityMeshTerrains.Count == 0 && mapGeometry.Count == 0 && waterObjects.Count == 0)
                    {
                        return;
                    }
                    // default property generation for max and min: halve whatever the terrain max/min are (since terrain generally exceeds the normal playable area by a bit), if terrain max is exceptionally large, quarter it, and ensure terrain min is at most zero (which is the vertical level of ship landing spot)
                    float minHeight = terrainMin < 0 ? (terrainMin / 2) : 0;
                    float maxHeight = fullHeight ? terrainMax : terrainMax > 100 ? (terrainMax / (Mathf.RoundToInt(terrainMax / 100) + 2)) : (terrainMax / 2);
                    MaterialProperties newProperties = new MaterialProperties(showGeometry, subtleObjects, lineSpace, stroke, minHeight, maxHeight, maxOpacity, multiplier, color, color);
                    newProperties.LogAllProperties();
                    if (contourDataDict.ContainsKey(moonIdentifier))
                    {
                        contourDataDict.Remove(moonIdentifier);
                    }
                    contourDataDict.Add(moonIdentifier, newProperties);// remember properties for next time
                    newProperties.SetProperties(contourMaterial);// updates contour shader with calculated values
                    SetupMeshes(subtleObjects);// create and assign materials to meshes
                }
                GameObject contourObj = GameObject.FindGameObjectWithTag("TerrainContourMap");
                if (contourObj == null)
                {
                    contourObj = GameObject.Find("Systems/Radar/RadarSquare");
                }
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

        public static void FetchTerrainObjects(bool hasMatInfo, (string,string) identifier, Bounds navMeshBounds)
        {
            terrainObjects.Clear();
            unityMeshTerrains.Clear();

            List<Terrain> unityTerrains = Object.FindObjectsOfType<Terrain>().Where(x => x.gameObject.activeInHierarchy && x.enabled && x.drawHeightmap && x.GetComponent<TerrainCollider>() && x.GetComponent<TerrainCollider>().enabled).ToList();

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

                MeshCollider[] meshColliders = Object.FindObjectsOfType<MeshCollider>();
                List<MeshRenderer> terrainRenderers = new List<MeshRenderer>();
                meshColliders.ForEach(x => CollectRenderers(x, terrainRenderers, 90000f, true, 1));
                if (terrainRenderers.Count == 0 && unityTerrains.Count == 0)
                {
                    UniversalRadar.Logger.LogWarning("Unable to find any terrain objects on this moon!");
                    return;
                }
                UniversalRadar.Logger.LogDebug($"First time load: Fetched {terrainRenderers.Count} terrain renderers");
                terrainMax = -10000;
                terrainMin = 10000;

                List<int> validTerrains = NavMeshBest(navMeshBounds, terrainRenderers.ConvertAll(x => x.bounds).ToArray(), unityTerrains.Count <= 0, 10);// send bounds of each terrain to function which returns the indices which are valid
                UniversalRadar.Logger.LogDebug($"{validTerrains.Count} valid mesh terrains");
                List<string> paths = new List<string>();
                for (int i = 0; i < terrainRenderers.Count; i++)
                {
                    if (validTerrains.Contains(i) && !terrainObjects.Contains(terrainRenderers[i].gameObject))
                    {
                        //UniversalRadar.Logger.LogDebug($"Terrain bounds ({GetObjectPath(terrainRenderers[i].gameObject)}): {terrainRenderers[i].bounds.size.x}, {terrainRenderers[i].bounds.size.y}, {terrainRenderers[i].bounds.size.z} - {terrainRenderers[i].bounds.size.magnitude}");
                        terrainObjects.Add(terrainRenderers[i].gameObject);
                        paths.Add(GetObjectPath(terrainRenderers[i].gameObject));
                        terrainMax = Mathf.Max(terrainRenderers[i].bounds.max.y, terrainMax);
                        terrainMin = Mathf.Min(terrainRenderers[i].bounds.min.y, terrainMin);
                    }
                }
                if (paths.Count > 0)
                {
                    UniversalRadar.Logger.LogDebug($"Adding terrain paths to dictionary under identifier ({identifier.Item1}, {identifier.Item2})");
                    terrainMemoryDict.Add(identifier, paths);// add terrain object paths for next cycle
                }
            }


            if (UniversalRadar.UseTerraMesh.Value && unityTerrains.Count > 0)// unity terrain objects need to be converted to actual meshes via TerraMesh, requires separate handling
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
                    MeshifyTerrains(unityTerrains, !hasMatInfo, identifier, navMeshBounds, terrainObjects.Count <= 0);// create new mesh terrain
                }
            }
            if (terrainMin > terrainMax)
            {
                terrainMax = navMeshBounds.max.y;
                terrainMin = navMeshBounds.min.y;
            }
            else
            {
                terrainMax = Mathf.Min(navMeshBounds.max.y, terrainMax);
                terrainMin = Mathf.Max(navMeshBounds.min.y, terrainMin);
            }
            UniversalRadar.Logger.LogDebug($"Terrain max: {terrainMax}, terrain min: {terrainMin} ({navMeshBounds.max.y}, {navMeshBounds.min.y})");
        }

        public static void FetchMapGeometry((string, string) identifier, Bounds navMeshBounds)
        {
            mapGeometry.Clear();

            if (geometryMemoryDict.TryGetValue(identifier, out List<string> values))// if game object paths are stored from previous generation
            {
                for (int i = 0; i < values.Count; i++)
                {
                    GameObject geometryObj = GameObject.Find(values[i]);
                    if (geometryObj != null && !mapGeometry.Contains(geometryObj) && (bool)geometryObj.GetComponent<MeshRenderer>())
                    {
                        mapGeometry.Add(geometryObj);
                    }
                }
            }
            else
            {
                Collider[] geometryColliders = Object.FindObjectsOfType<Collider>();
                List<MeshRenderer> geometryRenderers = new List<MeshRenderer>();
                geometryColliders.ForEach(x => CollectRenderers(x,geometryRenderers, UniversalRadar.RadarObjectSize.Value, false, 2));
                UniversalRadar.Logger.LogDebug($"First time load: Fetched {geometryRenderers.Count} object renderers");
                List<string> paths = new List<string>();
                for (int i = 0; i < geometryRenderers.Count; i++)
                {
                    if (geometryRenderers[i].gameObject != null && !mapGeometry.Contains(geometryRenderers[i].gameObject) && !terrainObjects.Contains(geometryRenderers[i].gameObject) && !unityMeshTerrains.Contains(geometryRenderers[i].gameObject) && WithinNavMesh(navMeshBounds, geometryRenderers[i].bounds))// not already added (or already considered terrain) and encapsulated by nav mesh
                    {
                        //UniversalRadar.Logger.LogDebug($"Object bounds ({GetObjectPath(geometryRenderers[i].gameObject)}): {geometryRenderers[i].bounds.size.x}, {geometryRenderers[i].bounds.size.y}, {geometryRenderers[i].bounds.size.z} > {geometryRenderers[i].bounds.size.x * geometryRenderers[i].bounds.size.z}");
                        UniversalRadar.Logger.LogDebug($"Adding map object {GetObjectPath(geometryRenderers[i].gameObject)}, {(bool)geometryRenderers[i].gameObject.GetComponent<MeshRenderer>()}");
                        mapGeometry.Add(geometryRenderers[i].gameObject);
                        paths.Add(GetObjectPath(geometryRenderers[i].gameObject));
                    }
                }
                if (paths.Count > 0)
                {
                    UniversalRadar.Logger.LogDebug($"Adding object paths to dictionary under identifier ({identifier.Item1}, {identifier.Item2})");
                    geometryMemoryDict.Add(identifier, paths);// add object paths for next cycle
                }
            }

        }

        public static void ClearRadarAddWater(bool skipWater)// the method to clean up any leftover radar objects and add water objects are merged so they can both take advantage of the same FindObjectsOfType<MeshRenderer>() check
        {
            bool findWater = UniversalRadar.RadarWater.Value;
            waterObjects.Clear();
            MeshRenderer[] renderers = Object.FindObjectsOfType<MeshRenderer>().Where(x => x.gameObject.name.EndsWith("_URRadarFill", System.StringComparison.Ordinal) || x.gameObject.name.EndsWith("_URContourMesh", System.StringComparison.Ordinal) || (!skipWater && findWater && x.sharedMaterial != null && x.sharedMaterial.name.ToLower().Contains("water", System.StringComparison.Ordinal))).ToArray();
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].gameObject.name.EndsWith("_URRadarFill", System.StringComparison.Ordinal) || renderers[i].gameObject.name.EndsWith("_URContourMesh", System.StringComparison.Ordinal))
                {
                    Object.DestroyImmediate(renderers[i].gameObject);
                }
                else if (renderers[i].sharedMaterial != null && renderers[i].sharedMaterial.name.ToLower().Contains("water", System.StringComparison.Ordinal) && renderers[i].sharedMaterial.shader != null && renderers[i].sharedMaterial.shader.name.ToLower().Contains("water", System.StringComparison.Ordinal))
                {
                    //UniversalRadar.Logger.LogDebug($"Found water object! {renderers[i].material.shader.name} - {GetObjectPath(renderers[i].gameObject)}");
                    waterObjects.Add(renderers[i].gameObject);
                }
            }
        }

        public static void MeshifyTerrains(List<Terrain> terrains, bool findBounds, (string, string) identifier, Bounds navMeshBounds, bool findAnyTerrain)// use TerraMesh to convert Unity terrain objects into actual meshes we can use
        {
            Shader tmShader = Shader.Find("HDRP/Lit");// random generic shader since we're immediately replacing terrain material anyways
            TerraMeshConfig tmConfig = new TerraMeshConfig(detailInstancingBatchSize: 1023, terraMeshShader: tmShader);
            List<MeshTerrainInfo> terrainInfos = new List<MeshTerrainInfo>();
            List<int> validTerrains = NavMeshBest(navMeshBounds, terrains.ConvertAll(x => x.GetComponent<TerrainCollider>().bounds).ToArray(), findAnyTerrain, 3);// send bounds of each terrain to function which returns the indices which are valid
            UniversalRadar.Logger.LogDebug($"{validTerrains.Count} valid unity terrains");
            for (int i = 0; i < terrains.Count; i++)
            {
                if (!validTerrains.Contains(i) || terrains[i] == null || terrains[i].terrainData == null)
                {
                    continue;
                }

                //UniversalRadar.Logger.LogDebug($"Unity terrain object at: {GetObjectPath(terrains[i].gameObject)}");
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
            if (terrainInfos.Count > 0)
            {
                meshTerrainDict.Add(identifier, terrainInfos);// save gathered info so we don't have to re-generate meshes next time
            }
        }

        public static void CollectRenderers(Collider collider, List<MeshRenderer> renderers, float minSize, bool terrain, int searchDepth)
        {
            if (IsValidMesh(collider, minSize, terrain, 0) && !renderers.Contains(collider.GetComponent<MeshRenderer>()))// renderer and collider share object
            {
                renderers.Add(collider.GetComponent<MeshRenderer>());
            }
            else if (searchDepth >= 1 && IsValidMesh(collider, minSize, terrain, 1) && !renderers.Contains(collider.transform.parent.GetComponent<MeshRenderer>()))// child colliders
            {
                renderers.Add(collider.transform.parent.GetComponent<MeshRenderer>());
            }
            else if (searchDepth >= 2 && IsValidMesh(collider, minSize, terrain, 2) && !renderers.Contains(collider.transform.parent.parent.GetComponent<MeshRenderer>()))// grandchild colliders
            {
                renderers.Add(collider.transform.parent.parent.GetComponent<MeshRenderer>());
            }
        }

        public static bool IsValidMesh(Collider collider, float minSize, bool terrain = true, int searchParents = 0)
        {
            GameObject colliderObj = collider.gameObject;
            bool foundRenderer = false;
            Transform targetTransform = colliderObj.transform;
            for (int i = 0; i <= searchParents; i++)
            {
                if (targetTransform.gameObject.GetComponent<MeshRenderer>() && i == searchParents)
                {
                    foundRenderer = true;
                    break;
                }
                else if (targetTransform.parent != null)
                {
                    targetTransform = targetTransform.transform.parent;
                }
                else
                {
                    break;
                }
            }
            if (!foundRenderer)
            {
                return false;
            }
            GameObject rendererObj = targetTransform.gameObject;
            MeshRenderer renderer = rendererObj.GetComponent<MeshRenderer>();
            if (!renderer.enabled)
            {
                return false;
            }
            if (collider.isTrigger)
            {
                return false;
            }
            if (rendererObj.layer != 8 && (rendererObj.layer != 10 || !showFoliage) && rendererObj.layer != 0 && (rendererObj.layer != 25 || !showFoliage) && (rendererObj.layer != 11 || terrain))
            {
                return false;
            }
            if (waterObjects.Contains(rendererObj))
            {
                return false;
            }

            if (rendererObj.name.Contains("_URContourMesh", System.StringComparison.Ordinal) || rendererObj.name.Contains("_URRadarFill", System.StringComparison.Ordinal))
            {
                return false;
            }
            if (rendererObj.name.ToLower().Contains("scannode", System.StringComparison.Ordinal) || (bool)rendererObj.GetComponent<ScanNodeProperties>())
            {
                return false;
            }
            if (GetObjectPath(colliderObj).Contains("Environment/HangarShip/", System.StringComparison.Ordinal))
            {
                return false;
            }
            if (GetObjectPath(colliderObj).ToLower().Contains("outofbounds", System.StringComparison.Ordinal))
            {
                return false;
            }

            if (!terrain && (rendererObj.name.Contains("LOD", System.StringComparison.Ordinal) || rendererObj.name.ToLower().Contains("lowdetail", System.StringComparison.Ordinal)))
            {
                if (rendererObj.transform.parent != null && rendererObj.transform.parent.GetComponent<LODGroup>() && rendererObj.transform.parent.GetComponent<MeshRenderer>())
                {
                    return false;
                }
            }
            if (!(bool)rendererObj.GetComponent<MeshFilter>())
            {
                return false;
            }
            if (terrain && (((bool)rendererObj.GetComponent<MeshFilter>() && rendererObj.GetComponent<MeshFilter>().sharedMesh != null && rendererObj.GetComponent<MeshFilter>().sharedMesh.name.ToLower().Contains("cube", System.StringComparison.Ordinal)) || (collider is MeshCollider meshCollider && meshCollider.sharedMesh != null && meshCollider.sharedMesh.name.ToLower().Contains("cube", System.StringComparison.Ordinal))))
            {
                return false;
            }
            if (rendererObj.GetComponentsInChildren<Component>(true).Any(x => x != null && blacklistTypes.Contains(x.GetType())))
            {
                return false;
            }

            if (!terrain && (GetObjectPath(colliderObj).ToLower().Contains("catwalk", System.StringComparison.Ordinal) || GetObjectPath(colliderObj).ToLower().Contains("bridge", System.StringComparison.Ordinal) || rendererObj.name.ToLower().Contains("floor", System.StringComparison.Ordinal)))
            {
                return true;
            }
            if (terrain && (colliderObj.name.ToLower().Contains("terrain", System.StringComparison.Ordinal) || rendererObj.name.ToLower().Contains("terrain", System.StringComparison.Ordinal)))
            {
                return true;
            }
            if (collider.bounds.size.x * collider.bounds.size.z > minSize || renderer.bounds.size.x * renderer.bounds.size.z > minSize)
            {
                return true;
            }
            return false;
        }

        public static bool WithinNavMesh(Bounds navBounds, Bounds objectBounds)
        {
            if (navBounds.max.x > objectBounds.center.x && navBounds.min.x < objectBounds.center.x && navBounds.max.z > objectBounds.center.z && navBounds.min.z < objectBounds.center.z)// 2D bounds check
            {
                return true;
            }
            else if (navBounds.Intersects(objectBounds))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        // will return a list of indices for which objects fit the 3 nav mesh criteria, and are added in order of the most suitable, so if sizeLimit is reached the best ones are the ones included
        // if useBest is set, even if none pass the criteria, the least bad one will be returned
        public static List<int> NavMeshBest(Bounds navBounds, Bounds[] boundsObjects, bool useBest, int sizeLimit)
        {
            // int determines whether it doesn't intersect (0), is too big (1), or passed all checks (2), higher is better
            // float is the relevant distance (from object to nav mesh, difference between sizes on axis, etc.), smaller is better
            (int, float) rank = (0, 1000000f);
            List<int> passedBounds = new List<int>();
            Dictionary<int, (int, float)> boundsRanked = new Dictionary<int, (int, float)>();
            for (int i = 0; i < boundsObjects.Length; i++)
            {
                Bounds objectBounds = boundsObjects[i];
                bool passed = true;
                //UniversalRadar.Logger.LogDebug($"terrain: {objectBounds.center}; nav: {navBounds.center}");

                if (!navBounds.Intersects(objectBounds))// terrain object does not intersect nav mesh
                {
                    //UniversalRadar.Logger.LogDebug($"EXCLUDING ({i}): Terrain does not intersect nav mesh bounds!");
                    passed = false;
                    float dist = Vector3.Distance(shipPos, objectBounds.center);
                    if (rank.Item1 == 0 && dist < rank.Item2)
                    {
                        rank = (0, dist);
                        boundsRanked.Add(i, rank);                 
                    }
                    continue;
                }
                //UniversalRadar.Logger.LogDebug($"terrain: {objectBounds.min.x}, {objectBounds.max.x}; nav: {navBounds.min.x}, {navBounds.max.x}");
                if ((objectBounds.max.x > navBounds.max.x || objectBounds.min.x < navBounds.min.x) && objectBounds.size.x - navBounds.size.x > navBounds.size.x * 15)// terrain x-bounds >1500% bigger than nav region
                {
                    //UniversalRadar.Logger.LogDebug($"EXCLUDING ({i}): x-dimension of terrain significantly exceeds nav mesh bounds!");
                    passed = false;
                    float diff = objectBounds.size.x - navBounds.size.x;
                    if (rank.Item1 == 0 || diff < rank.Item2)
                    {
                        rank = (1, diff);
                    }
                }
                //UniversalRadar.Logger.LogDebug($"terrain: {objectBounds.min.z}, {objectBounds.max.z}; nav: {navBounds.min.z}, {navBounds.max.z}");
                if ((objectBounds.max.z > navBounds.max.z || objectBounds.min.z < navBounds.min.z) && objectBounds.size.z - navBounds.size.z > navBounds.size.z * 15)// terrain z-bounds >1500% bigger than nav region
                {
                    //UniversalRadar.Logger.LogDebug($"EXCLUDING ({i}): z-dimension of terrain significantly exceeds nav mesh bounds!");
                    passed = false;
                    float diff = objectBounds.size.z - navBounds.size.z;
                    if (rank.Item1 == 0 || diff < rank.Item2)
                    {
                        rank = (1, diff);
                    }
                }
                if (passed)
                {
                    boundsRanked.Add(i, (2, Vector3.Distance(shipPos, objectBounds.center)));
                }
                else
                {
                    boundsRanked.Add(i, rank);
                }
            }

            boundsRanked = boundsRanked.OrderByDescending(x => x.Value.Item1).ThenBy(x => x.Value.Item2).ToDictionary(x => x.Key, x => (x.Value.Item1,x.Value.Item2));

            foreach (var entry in boundsRanked)
            {
                if (entry.Value.Item1 == 2 && passedBounds.Count < sizeLimit)
                {
                    passedBounds.Add(entry.Key);
                }
            }

            if (useBest && passedBounds.Count <= 0 && boundsRanked.Count > 0)
            {
                passedBounds.Add(boundsRanked.First().Key);
            }
            return passedBounds;
        }

        public static void SetupMeshes(bool altMat)
        {
            for (int i = 0; i < terrainObjects.Count; i++)// instantiate new objects and set up their parameters for contour shader material
            {
                //UniversalRadar.Logger.LogDebug($"Terrain object at: {GetObjectPath(terrainObjects[i])}");
                GameObject contourTerrain = Object.Instantiate(terrainObjects[i], terrainObjects[i].transform.position, terrainObjects[i].transform.rotation, terrainObjects[i].transform.parent);
                contourTerrain.name = terrainObjects[i].name + "_URContourMesh";
                contourTerrain.layer = 14;
                CleanComponents(contourTerrain);
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
                //UniversalRadar.Logger.LogDebug($"Unity terrain object at: {GetObjectPath(unityMeshTerrains[i])}");
                unityMeshTerrains[i].name = unityMeshTerrains[i].name + "_URContourMesh";
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
            for (int i = 0; i < mapGeometry.Count; i++)// instantiate basic green fills for other non-terrain geometry
            {
                if (mapGeometry[i] == null) { continue; }
                //UniversalRadar.Logger.LogDebug($"Map geometry object at: {GetObjectPath(mapGeometry[i])}");
                GameObject geometryFill = Object.Instantiate(mapGeometry[i], mapGeometry[i].transform.position, mapGeometry[i].transform.rotation, mapGeometry[i].transform.parent);
                geometryFill.name = mapGeometry[i].name + "_URRadarFill";
                geometryFill.layer = 14;
                CleanComponents(geometryFill);
                MeshRenderer geometryRenderer = geometryFill.GetComponent<MeshRenderer>();
                Material[] materials = geometryRenderer.materials;
                Material fillMat = altMat ? radarFillMat1 : radarFillMat0;
                for (int j = 0; j < materials.Length; j++)
                {
                        materials[j] = fillMat;
                }
                geometryFill.transform.position += verticalOffset;
                geometryRenderer.materials = materials;
            }
            for (int i = 0; i < waterObjects.Count; i++)// same as above but for water
            {
                //UniversalRadar.Logger.LogDebug($"Water object at: {GetObjectPath(waterObjects[i])}");
                GameObject waterFill = Object.Instantiate(waterObjects[i], waterObjects[i].transform.position, waterObjects[i].transform.rotation, waterObjects[i].transform.parent);
                waterFill.name = waterObjects[i].name + "_URRadarFill";
                waterFill.layer = 14;
                CleanComponents(waterFill);
                Material[] materials = waterFill.GetComponent<MeshRenderer>().materials;
                for (int j = 0; j < materials.Length; j++)
                {
                    materials[j] = radarWaterMat;
                }
                waterFill.transform.position += verticalOffset;
                waterFill.GetComponent<MeshRenderer>().materials = materials;
            }
        }

        public static void CleanComponents(GameObject obj)
        {
            if (obj == null) { return; }
            GameObject[] allObj = obj.GetComponentsInChildren<GameObject>();
            foreach (GameObject child in allObj)
            {
                if (child != obj)
                {
                    Object.Destroy(child);
                }
                else
                {
                    foreach (Component component in child.GetComponents<Component>())
                    {
                        if (component != null && disableTypes.Contains(component.GetType()) && component is Behaviour componentBehaviour)
                        {
                            componentBehaviour.enabled = false;
                        }
                        if (component != null && component is LODGroup lod)
                        {
                            lod.enabled = false;
                        }
                    }
                }
            }
        }

        public static string GetObjectPath(GameObject obj)
        {
            if (obj == null) { return ""; }
            StringBuilder path = new StringBuilder(obj.name);
            Transform current = obj.transform.parent;

            while (current != null)
            {
                path.Insert(0, current.name + "/");
                current = current.parent;
            }

            return path.ToString();
        }

        public static Vector4 ColourFromHex(string hexCode)
        {
            string hex = hexCode.Replace("#", "").ToUpper();
            if (hex.Length == 6 && Regex.Match(hex, "^[A-F0-9]{6}$").Success)
            {
                float R = System.Convert.ToInt32(hex.Substring(0, 2), 16) / 255f;
                float G = System.Convert.ToInt32(hex.Substring(2, 2), 16) / 255f;
                float B = System.Convert.ToInt32(hex.Substring(4, 2), 16) / 255f;
                return new Vector4(R, G, B, 1f);
            }
            return new Vector4(-1f, -1f, -1f, -1f);
        }

        public static string HexFromColour(Color color)
        {
            string R = color.r.ToString("X2");
            string G = color.g.ToString("X2");
            string B = color.b.ToString("X2");
            return "#" + R + G + B;
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
        public bool auto;
        public bool showObjects;
        public bool lowObjectOpacity;
        public bool extendHeight;
        public float lineSpacing;
        public float lineThickness;
        public float minHeight;
        public float maxHeight;
        public float opacityCap;
        public float opacityMult;
        public Vector4 baseColour;
        public Vector4 lineColour;

        public MaterialProperties(bool show, bool lowOpacity, float spacing, float thickness, float min, float max, float opacity, float multiplier, Vector4 colourBG, Vector4 colourLine)
        {
            auto = false;
            showObjects = show;
            lowObjectOpacity = lowOpacity;
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
                auto = true;
                showObjects = propertiesConfig.showObjects.Value;
                lowObjectOpacity = propertiesConfig.lowObjectOpacity.Value;
                extendHeight = propertiesConfig.extendHeight.Value;
                opacityMult = propertiesConfig.opacityMult.Value;

                Vector4 colourBG = RadarContourPatches.ColourFromHex(propertiesConfig.baseColourHex.Value);
                baseColour = RadarContourPatches.defaultGreen;
                if (colourBG.x >= 0)
                {
                    baseColour = colourBG;
                }
            }
            else
            {
                auto = false;
                showObjects = propertiesConfig.showObjects.Value;
                lowObjectOpacity = propertiesConfig.lowObjectOpacity.Value;
                extendHeight = false;
                lineSpacing = propertiesConfig.lineSpacing.Value;
                lineThickness = propertiesConfig.lineThickness.Value;
                minHeight = propertiesConfig.minHeight.Value;
                maxHeight = propertiesConfig.maxHeight.Value;
                opacityCap = propertiesConfig.opacityCap.Value;
                opacityMult = propertiesConfig.opacityMult.Value;

                Vector4 colourBG = RadarContourPatches.ColourFromHex(propertiesConfig.baseColourHex.Value);
                baseColour = RadarContourPatches.defaultGreen;
                if (colourBG.x >= 0)
                {
                    baseColour = colourBG;
                }
                Vector4 colourLine = RadarContourPatches.ColourFromHex(propertiesConfig.lineColourHex.Value);
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
            if (UniversalRadar.LogValues.Value)
            {
                UniversalRadar.Logger.LogInfo($"SPACING: {lineSpacing}");
                UniversalRadar.Logger.LogInfo($"THICKNESS: {lineThickness}");
                UniversalRadar.Logger.LogInfo($"MIN: {minHeight}");
                UniversalRadar.Logger.LogInfo($"MAX: {maxHeight}");
                UniversalRadar.Logger.LogInfo($"MAX OPACITY: {opacityCap}");
                UniversalRadar.Logger.LogInfo($"OPACITY MULT: {opacityMult}");
            }
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

            if (Vector4.Distance(lineColour,RadarContourPatches.defaultGreen) > 0.01f)
            {
                for (int i = 0; i < 4; i++)
                {
                    float red = Mathf.Clamp(lineColour[0] * 2.5f, 0f, 1f);
                    float green = Mathf.Clamp(lineColour[1] * 2.5f, 0f, 1f);
                    float blue = Mathf.Clamp(lineColour[2] * 2.5f, 0f, 1f);
                    Vector4 newColor = new Vector4(red, green, blue, 1f);
                    RadarContourPatches.radarFillMat0.color = newColor;
                    RadarContourPatches.radarFillMat1.color = newColor;
                }
            }
            else
            {
                RadarContourPatches.radarFillMat0.color = RadarContourPatches.radarFillGreen;
                RadarContourPatches.radarFillMat1.color = RadarContourPatches.radarFillGreen;
            }
            contourMat.SetColor("_BaseColor", baseColour);
            contourMat.SetColor("_LineColor", lineColour);
        }
    }
}
