using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace UniversalRadar.Patches
{
    [HarmonyPatch]
    public class RadarExtraPatches
    {
        private static ManualCameraRenderer twoRadarCam;

        [HarmonyPatch(typeof(ManualCameraRenderer), nameof(ManualCameraRenderer.MapCameraFocusOnPosition))]
        [HarmonyPostfix]
        static void ClippingPatch(ManualCameraRenderer __instance)
        {
            if (RadarContourPatches.disableMoon) { return; }

            if (__instance.cam == __instance.mapCamera)
            {
                SetClipping(__instance);
            }
        }

        static void SetClipping(ManualCameraRenderer radarCam)// clipping plane is normally so tight that only a narrow vertical band around player is captured, so it needs to be extended to capture the terrain's contour map
        {
            int index = radarCam.targetTransformIndex;
            if (radarCam.targetedPlayer == null)
            {
                index = -1;
            }
            // only increase clipping when player/radar booster is outside
            if (index != -1 && (!radarCam.targetedPlayer.isInsideFactory || (radarCam.radarTargets[index].isNonPlayer && (bool)radarCam.radarTargets[index].transform.GetComponent<RadarBoosterItem>() && !radarCam.radarTargets[index].transform.GetComponent<RadarBoosterItem>().isInFactory)))
            {
                radarCam.mapCamera.nearClipPlane -= UniversalRadar.CameraClipExtension.Value;
                radarCam.mapCamera.farClipPlane += UniversalRadar.CameraClipExtension.Value;
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

            if (!displayInfo)// when radar enabled
            {// fixes an issue where destroyed contour maps would prevent new ones from being loaded in (unsure if vanilla issue or modded one)
                FieldInfo field = AccessTools.Field(typeof(ManualCameraRenderer), "checkedForContourMap");
                if (field != null)
                {
                    field.SetValue(__instance.mapScreen, false);
                }
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

        public static void AddNewRadarSprites((string, string) identifier)// disable existing map radar objects and replace them with my custom-made ones (for vanilla)
        {
            string sceneName = identifier.Item2;
            if (!ConfigPatch.vanillaSceneDict.ContainsValue(sceneName) || (UniversalRadar.dopaPresent && sceneName.StartsWith("Re") && !sceneName.Contains("Level")) || UniversalRadar.spookyPresent) { return; }// skip sprites for non-vanilla moons or Rebalanced Moons or if MapImprovements installed
            string moonName = identifier.Item1;
            if (sceneName == "Level4March" || sceneName == "Level8Titan")// create dummy objects for moons lacking contour maps
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

                    if (moonName == "7 Dine" && UniversalRadar.terraformerPresent)// TonightWeDine edge case
                    {
                        GameObject mainObj = GameObject.Find("DineRadarSprites(Clone)/Main");
                        GameObject bgObj = GameObject.Find("DineRadarSprites(Clone)/MainBG");
                        mainObj.SetActive(false);
                        bgObj.SetActive(false);
                    }
                }
            }
        }
    }
}
