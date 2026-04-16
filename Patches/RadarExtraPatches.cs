using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UniversalRadar.Patches
{
    [HarmonyPatch]
    public class RadarExtraPatches
    {
        public static ManualCameraRenderer twoRadarCam;
        public static Vector4 camColour = new Vector4(0f, 0.02745098f, 0.003921569f, 0f);
        public static Vector4 shipColour = new Vector4(0.3882353f, 1f, 0.3529412f, 1f);
        private static readonly Color greenTransition1 = new Color(0.02309575f, 0.3113208f, 0f, 1f);
        private static readonly Color greenTransition2 = new Color(0.08566217f, 0.3301887f, 0.08566217f, 1f);
        private static readonly string[] companyRadarObjects = ["CompanyBuildingContour2", "CompanyBuildingContour2 (2)", "CompanyBuildingContourStairs", "CompanyBuildingContourStairs (1)", "CompanyBuildingContourStairs (2)", "CompanyBuildingContourStairs (4)"];
        public static bool alteredTransition = false;

        [HarmonyPatch(typeof(ManualCameraRenderer), nameof(ManualCameraRenderer.MapCameraFocusOnPosition))]
        [HarmonyPostfix]
        static void ClippingPatch(ManualCameraRenderer __instance)
        {
            if (RadarContourPatches.disableMoon && StartOfRound.Instance.currentLevel.PlanetName != "71 Gordion") { return; }

            if (__instance.cam == __instance.mapCamera)
            {
                SetClipping(__instance);
            }
        }

        static void SetClipping(ManualCameraRenderer radarCam)// clipping plane is normally so tight that only a narrow vertical band around player is captured, so it needs to be extended to capture the terrain's contour map
        {
            int index = radarCam.targetTransformIndex;
            // only increase clipping when player/radar booster is outside
            if ((radarCam.targetedPlayer != null && !radarCam.targetedPlayer.isInsideFactory) || (radarCam.radarTargets[index].isNonPlayer && (bool)radarCam.radarTargets[index].transform.GetComponent<RadarBoosterItem>() && !radarCam.radarTargets[index].transform.GetComponent<RadarBoosterItem>().isInFactory))
            {
                if (radarCam.targetedPlayer != null && !radarCam.targetedPlayer.isInHangarShipRoom && StartOfRound.Instance.currentLevel.PlanetName == "71 Gordion")// company moon needs closer clip plane for its geometry
                {
                    radarCam.mapCamera.nearClipPlane = radarCam.cameraNearPlane - UniversalRadar.CameraClipExtension.Value;
                    radarCam.mapCamera.farClipPlane = radarCam.cameraFarPlane + UniversalRadar.CameraClipExtension.Value;
                }
                else
                {
                    radarCam.mapCamera.nearClipPlane -= UniversalRadar.CameraClipExtension.Value;
                    radarCam.mapCamera.farClipPlane += UniversalRadar.CameraClipExtension.Value;
                }
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SwitchMapMonitorPurpose))]
        [HarmonyPostfix]
        static void OnRadarEnable(StartOfRound __instance, bool displayInfo)
        {
            if (UniversalRadar.zaggyPresent && twoRadarCam == null)
            {
                twoRadarCam = Object.FindObjectOfType<Terminal>().GetComponent<ManualCameraRenderer>();
            }

            //__instance.mapScreen.checkedForContourMap = false;// fixes an issue where contour maps would not be fetched correctly

            SetCamParameters(displayInfo, __instance.mapScreen);
            if (twoRadarCam != null)
            {
                SetCamParameters(displayInfo, twoRadarCam);
            }

            if (RadarContourPatches.disableMoon) { return; }

            ToggleShipIcon(__instance.mapScreen, displayInfo);// turn off if radar is being enabled, turn back on if radar is being disabled
            if (twoRadarCam != null)
            {
                ToggleShipIcon(twoRadarCam, displayInfo);
            }
        }

        static void ToggleShipIcon(ManualCameraRenderer radarCam, bool show)// toggle the (normally invisible) ship icon
        {
            if (radarCam.shipArrowUI != null)
            {
                Transform shipUI = radarCam.shipArrowUI.transform.Find("ShipIcon");
                if (shipUI != null)
                {
                    if (!show)
                    {
                        shipUI.gameObject.SetActive(false);
                    }
                    else
                    {
                        shipUI.gameObject.SetActive(true);
                    }
                }
            }
        }

        static void SetCamParameters(bool reset, ManualCameraRenderer camScript)
        {
            if (!reset)// when radar enabled
            {

                if (!UniversalRadar.ShipInheritColour.Value && UniversalRadar.ShipColour.Value != "63FF5A")// set ship colour if inheritance is off
                {
                    SetShipSpriteColour(UniversalRadar.ShipColour.Value);
                }

                if (!RadarContourPatches.disableMoon)
                {
                    if (UniversalRadar.ShipInheritColour.Value && shipColour != ConfigPatch.defaultRadarGreen)// set ship colour based on radar objects (setting colour based on vanilla sprites is done in a different place)
                    {
                        GameObject shipSprite = GameObject.Find("Environment/HangarShip/Square");
                        if (shipSprite != null && shipSprite.GetComponent<SpriteRenderer>())
                        {
                            shipSprite.GetComponent<SpriteRenderer>().color = (Color)shipColour;
                        }
                    }
                    if (Vector4.Distance(camColour, ConfigPatch.defaultCameraGreen) > 0.01f)// set background and transition values
                    {
                        Color bgColour = (Color)camColour;
                        HDAdditionalCameraData camData = camScript.cam.gameObject.GetComponent<HDAdditionalCameraData>();
                        if (camData != null)
                        {
                            //UniversalRadar.Logger.LogDebug($"CHANGING CAM BG: {camData.backgroundColorHDR} > {bgColour}");
                            camData.backgroundColorHDR = bgColour;
                        }
                        Animator transitionAnim = camScript.cam.gameObject.GetComponentInChildren<Animator>();
                        if (transitionAnim != null && transitionAnim.GetComponent<MeshRenderer>())
                        {
                            Material transitionMat = transitionAnim.GetComponent<MeshRenderer>().sharedMaterial;
                            Color transition1 = bgColour.RGBMultiplied(15f);
                            transition1.a = 1f;
                            Color transition2 = bgColour.RGBMultiplied(20f);
                            transition2.a = 1f;
                            transitionMat.color = transition1;
                            if (transitionMat.HasColor("_EmissiveColor"))
                            {
                                transitionMat.SetColor("_EmissiveColor", transition2);
                            }
                            alteredTransition = true;
                        }
                    }
                }
            }
            else// when radar is disabled (e.g. going back into orbit)
            {
                GameObject shipSprite = GameObject.Find("Environment/HangarShip/Square");
                if (shipSprite != null && shipSprite.GetComponent<SpriteRenderer>())// reset ship sprite colour
                {
                    shipSprite.GetComponent<SpriteRenderer>().color = (Color)ConfigPatch.defaultRadarGreen;
                }
                HDAdditionalCameraData camData = camScript.cam.gameObject.GetComponent<HDAdditionalCameraData>();
                if (camData != null)// reset radar background colour
                {
                    camData.backgroundColorHDR = (Color)ConfigPatch.defaultCameraGreen;
                }
                if (alteredTransition)// reset transition animation colour
                {
                    Animator transitionAnim = camScript.cam.gameObject.GetComponentInChildren<Animator>();
                    if (transitionAnim != null && transitionAnim.GetComponent<MeshRenderer>())
                    {
                        Material transitionMat = transitionAnim.GetComponent<MeshRenderer>().sharedMaterial;
                        transitionMat.color = greenTransition1;
                        if (transitionMat.HasColor("_EmissiveColor"))
                        {
                            transitionMat.SetColor("_EmissiveColor", greenTransition2);
                        }
                        alteredTransition = false;
                    }
                }
            }
        }

        public static void SetShipSpriteColour(string colourHex)// set ship colour from a hex colour string
        {
            Vector4 newColour = ColourProperties.ColourFromHex(colourHex);
            if (newColour.x >= 0)
            {
                GameObject shipSprite = GameObject.Find("Environment/HangarShip/Square");
                if (shipSprite != null && shipSprite.GetComponent<SpriteRenderer>())
                {
                    shipSprite.GetComponent<SpriteRenderer>().color = (Color)newColour;
                }
            }
        }

        public static void AddNewRadarSprites((string, string) identifier)// disable existing map radar objects and replace them with custom-made ones (for vanilla moons)
        {
            string sceneName = identifier.Item2;
            if (!ConfigPatch.vanillaSceneDict.ContainsValue(sceneName) || UniversalRadar.spookyPresent) { return; }// skip sprites for non-vanilla moons or if MapImprovements installed
            if (UniversalRadar.dopaPresent && sceneName.StartsWith("Re") && !sceneName.Contains("Level"))// re-balanced moons case
            {
                if (ConfigPatch.radarSpritePrefabs.TryGetValue(sceneName, out (GameObject, string) spriteEntry) && spriteEntry.Item2 != "63FF5A")// colour patch for RMB specifically since I can't change colour in advance like with my own prefabs
                {
                    // this is the reason why I store the hex string in the radar sprite prefab dictionary
                    string colourHex = spriteEntry.Item2;
                    if (UniversalRadar.ShipInheritColour.Value && spriteEntry.Item2 != "63FF5A")
                    {
                        SetShipSpriteColour(colourHex);
                    }
                    Color spriteColour = (Color)ColourProperties.ColourFromHex(colourHex);
                    Color spriteColourDark = spriteColour.RGBMultiplied(0.5f);
                    Color spriteColourDarker = spriteColour.RGBMultiplied(0.4f);
                    string prefabName = ConfigPatch.radarSpritePrefabs[ConfigPatch.vanillaSceneDict[identifier.Item1]].Item1.name;
                    GameObject spritePrefab = GameObject.Find(prefabName);
                    SpriteRenderer[] sprites = spritePrefab.GetComponentsInChildren<SpriteRenderer>();
                    for (int i = 0; i < sprites.Length; i++)
                    {
                        if (sprites[i].color == (Color)ConfigPatch.defaultRadarGreen)
                        {
                            sprites[i].color = spriteColour;
                        }
                        else if (sprites[i].color == (Color)ConfigPatch.defaultSpriteGreenDark)
                        {
                            sprites[i].color = spriteColourDark;
                        }
                        else if (sprites[i].color == (Color)ConfigPatch.defaultSpriteGreenDarker)
                        {
                            sprites[i].color = spriteColourDarker;
                        }
                    }
                }
                return;
            }
            string moonName = identifier.Item1;
            if (sceneName == "Level8Titan" || sceneName == "CompanyBuilding")// create dummy objects for moons lacking contour maps
            {
                GameObject contourMap = new GameObject("ContourMap");
                Transform environment = GameObject.Find("Environment/BoundsWalls").transform.parent;// we use this since finding "Environment" directly can conflict with the Environment object in the ship SampleScene
                contourMap.transform.SetParent(environment);
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
                if (sceneName == "CompanyBuilding")// gordion has radar sprites but no proper container for them, so we disable them manually
                {
                    foreach (string objName in companyRadarObjects)
                    {
                        GameObject contourSprite = GameObject.Find("Environment/" + objName);
                        if (contourSprite != null && contourSprite.GetComponent<SpriteRenderer>())
                            contourSprite.GetComponent<SpriteRenderer>().enabled = false;
                    }
                }
                bool backup = true;
                if (ConfigPatch.radarSpritePrefabs.TryGetValue(sceneName, out (GameObject, string) value1))// regular sprites
                {
                    backup = false;
                    if (UniversalRadar.ShipInheritColour.Value && value1.Item2 != "63FF5A")
                    {
                        SetShipSpriteColour(value1.Item2);
                    }
                    SpriteRenderer[] existingSprites = contourObj.GetComponentsInChildren<SpriteRenderer>();
                    for (int i = 0; i < existingSprites.Length; i++)
                    {
                        existingSprites[i].enabled = false;
                    }
                    GameObject newSprites = Object.Instantiate(value1.Item1, contourObj.transform);
                    newSprites.transform.localPosition = Vector3.zero;
                    newSprites.transform.localRotation = Quaternion.identity;
                    newSprites.transform.localScale = Vector3.one;

                    if (moonName == "7 Dine" && UniversalRadar.terraformerPresent)// TonightWeDine edge case
                    {
                        GameObject mainObj = GameObject.Find("DineRadarSprites(Clone)/Main");
                        GameObject bgObj = GameObject.Find("DineRadarSprites(Clone)/MainBG");
                        mainObj.SetActive(false);
                        bgObj.SetActive(false);
                    }
                }
                if (ConfigPatch.fixedRadarSpritePrefabs.TryGetValue(sceneName, out (GameObject, string) value2))// static sprites
                {
                    if (backup && UniversalRadar.ShipInheritColour.Value && value2.Item2 != "63FF5A")// set ship colour if there were no regular sprites
                    {
                        SetShipSpriteColour(value2.Item2);
                    }
                    Transform contourContainer = contourObj.transform.parent;// put this in the parent ContourMap object, which means it isn't tethered to the player
                    GameObject newSprites = Object.Instantiate(value2.Item1, contourContainer);
                    newSprites.transform.localPosition = Vector3.zero;
                    newSprites.transform.localRotation = Quaternion.identity;
                    newSprites.transform.localScale = Vector3.one;
                }
            }
        }
    }
}
