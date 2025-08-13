using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using DunGen;
using HarmonyLib;
using UnityEngine;

namespace UniversalRadar.Patches
{
    [HarmonyPatch]
    public class RadarFallbackPatches
    {
        public static bool interiorHasRadar = true;
        public static int newMask = 16640;
        public static int originalMask = 16384;
        private static bool done = false;

        [HarmonyPatch(typeof(ManualCameraRenderer), nameof(ManualCameraRenderer.Start))]
        [HarmonyPostfix]
        static void SetMask(ManualCameraRenderer __instance)
        {
            if (UniversalRadar.OldRadarFallback.Value && __instance.cam == __instance.mapCamera)
            {
                originalMask = __instance.cam.cullingMask;
                newMask = 1 << 14;// map radar layer
                newMask |= (1 << 8);// include room layer
                if (UniversalRadar.UseDefaultLayer.Value)
                    newMask |= (1 << 0);// include default layer
            }
        }

        [HarmonyPatch(typeof(ManualCameraRenderer), nameof(ManualCameraRenderer.Update))]
        [HarmonyPostfix]
        static void UpdateMask(ManualCameraRenderer __instance)
        {
            if (UniversalRadar.OldRadarFallback.Value && !StartOfRound.Instance.inShipPhase && __instance.cam == __instance.mapCamera && __instance.cam != null)
            {
                // the second headMountedCam check is for radar boosters which lack a player object
                if (!interiorHasRadar && ((__instance.targetedPlayer != null && __instance.targetedPlayer.isInsideFactory && __instance.targetedPlayer.transform.position.y < -80f) || (__instance.headMountedCamTarget != null && __instance.headMountedCamTarget.transform.position.y < -80f)))
                {
                    __instance.cam.cullingMask = newMask;
                }
                else
                {
                    __instance.cam.cullingMask = originalMask;
                }
            }
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.FinishGeneratingNewLevelClientRpc))]
        [HarmonyPostfix]
        static void AfterInteriorGen()// this is the detection algorithm to see if an interior has radar sprites
        {
            if (!UniversalRadar.OldRadarFallback.Value || done) { return; }
            UniversalRadar.SetTime();
            done = true;
            interiorHasRadar = false;
            GameObject genRoot = GameObject.Find("Systems/LevelGeneration/LevelGenerationRoot");
            if (genRoot == null) { return; }
            List<GameObject> interiorRadarObjects = GetAllChildrenOnLayer(genRoot.transform, 14);// get all objects in dungeon on mapradar layer
            List<GameObject> interiorSprites = interiorRadarObjects.Where(x => x.CompareTag("RadarRoomSprite") || x.tag == "RadarRoomSprite").ToList();

            //foreach (GameObject spriteObj in interiorSprites)
            //{
            //    UniversalRadar.Logger.LogDebug($"VALID ROOM SPRITES: {GetObjectPath(spriteObj)}");
            //}
            //foreach (GameObject spriteObj in largeInteriorObjects)
            //{
            //    UniversalRadar.Logger.LogDebug($"SPRITE: {GetObjectPath(spriteObj)} - SIZE: {spriteObj.GetComponent<SpriteRenderer>().bounds.size}");
            //}

            if (interiorRadarObjects.Count < 5)// if there's less than 5 objects on the mapradar layer, the interior doesn't have radar
            {
                UniversalRadar.Logger.LogDebug($"Interior radar check complete ({UniversalRadar.GetTime()}s)");
                return; 
            }
            if (interiorSprites.Count < 5)// if there's less than 5 properly tagged room sprites, try and determine if the interior creator just didn't tag their radar, or if they're actually lacking sufficient radar sprites
            {
                //UniversalRadar.Logger.LogDebug("CAUGHT");
                int tileCount = Object.FindObjectsOfType<Tile>().Count();
                if (1 - ((float)interiorRadarObjects.Count / (float)tileCount) > 0.1f)// if radar objects are less than 90% of tile count (i.e. if there's less than 9 radar objects for every 10 tiles), the interior radar isn't sufficient
                {// 90% might sound overly strict, but one problem is if the interior includes "radar blockers" to black out certain sections, these might cover a lot of rooms, but aren't actually useful radar sprites, so I try to be strict to avoid those sorts of extra sprites tricking the algorithm into thinking the radar is real
                    UniversalRadar.Logger.LogDebug($"Interior radar check complete ({UniversalRadar.GetTime()}s)");
                    return;
                }
                //UniversalRadar.Logger.LogDebug("PASS 1");

                // more strict version of the above check, now excluding radar objects too small to be a room sprite
                List<GameObject> largeInteriorObjects = interiorRadarObjects.Where(x => x.GetComponent<SpriteRenderer>() && x.GetComponent<SpriteRenderer>().bounds.size.x * x.GetComponent<SpriteRenderer>().bounds.size.z > 2f).ToList();
                if (1 - ((float)largeInteriorObjects.Count / (float)tileCount) > 0.1f)// if large radar objects (generally excludes stuff like doorways) are less than 90% of tile count, the interior radar isn't sufficient
                {
                    UniversalRadar.Logger.LogDebug($"Interior radar check complete ({UniversalRadar.GetTime()}s)");
                    return;
                }
                //UniversalRadar.Logger.LogDebug("PASS 2 ESCAPE");
            }
            interiorHasRadar = true;
            UniversalRadar.Logger.LogDebug($"Interior radar check complete ({UniversalRadar.GetTime()}s)");
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.DespawnPropsAtEndOfRound))]
        [HarmonyPostfix]
        static void ResetValues()
        {
            interiorHasRadar = true;
            done = false;
        }

        public static List<GameObject> GetAllChildrenOnLayer(Transform parent, int targetLayer)
        {
            List<GameObject> objectsList = new List<GameObject>();
            CollectChildrenOnLayer(parent, targetLayer, objectsList);
            return objectsList;
        }

        private static void CollectChildrenOnLayer(Transform current, int targetLayer, List<GameObject> objectsList)
        {
            for (int i = 0; i < current.childCount; i++)
            {
                Transform child = current.GetChild(i);

                if (child.gameObject.layer == targetLayer)
                {
                    objectsList.Add(child.gameObject);
                }
                CollectChildrenOnLayer(child, targetLayer, objectsList);
            }
        }

        //private static string GetObjectPath(GameObject obj)
        //{
        //    StringBuilder path = new StringBuilder(obj.name);
        //    Transform current = obj.transform.parent;

        //    while (current != null)
        //    {
        //        path.Insert(0, current.name + "/");
        //        current = current.parent;
        //    }

        //    return path.ToString();
        //}
    }
}
