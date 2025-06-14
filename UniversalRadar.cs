using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace UniversalRadar
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]// ScienceBird.UniversalRadar, UniversalRadar
    [BepInDependency("voxx.TerraMesh", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("imabatby.lethallevelloader", BepInDependency.DependencyFlags.SoftDependency)]
    public class UniversalRadar : BaseUnityPlugin
    {
        public static UniversalRadar Instance { get; private set; } = null!;
        internal new static ManualLogSource Logger { get; private set; } = null!;
        internal static Harmony? Harmony { get; set; }

        public static AssetBundle URAssets;

        public static ConfigEntry<float> AutoLineWidth, AutoSpacing, AutoOpacity, AutoMultiplier;
        public static ConfigEntry<float> CameraClipExtension;
        public static ConfigEntry<bool> ExperimentationSprites, AssuranceSprites, VowSprites, MarchSprites, RendSprites, DineSprites, OffenseSprites, TitanSprites, ArtificeSprites, AdamanceSprites, EmbrionSprites;
        public static ConfigEntry<bool> WaterSprites;

        public static Dictionary<string, GameObject> radarSpritePrefabs = new Dictionary<string, GameObject>();

        public static bool batbyPresent = false;

        private void Awake()
        {
            Logger = base.Logger;
            Instance = this;

            AutoSpacing = base.Config.Bind("Automatic Settings", "Line Spacing", 2.5f, new ConfigDescription("Spacing between lines in automatically generated contour maps.", new AcceptableValueRange<float>(0.5f, 6f)));
            AutoLineWidth = base.Config.Bind("Automatic Settings", "Line Thickness", 3.5f, new ConfigDescription("Thickness of lines in automatically generated contour maps.", new AcceptableValueRange<float>(0.5f, 8f)));
            AutoOpacity = base.Config.Bind("Automatic Settings", "Maximum Opacity", 1f, new ConfigDescription("Maximum opacity of the shading on automatically generated contour maps (how light the tallest parts of the contour map will be).", new AcceptableValueRange<float>(0.1f, 1f)));
            AutoMultiplier = base.Config.Bind("Automatic Settings", "Opacity Multiplier", 2f, new ConfigDescription("Opacity multiplier of the shading on automatically generated contour maps (all shading levels will be multiplied by this number, set higher to make shading generally lighter/higher contrast).", new AcceptableValueRange<float>(0.1f, 5f)));

            ExperimentationSprites = base.Config.Bind("Radar Sprites", "Experimentation", true, "Adds new radar sprites for Experimentation, showing buildings, obstacles, catwalks, and more (will replace any existing extra radar objects).");
            AssuranceSprites = base.Config.Bind("Radar Sprites", "Assurance", true, "Adds new radar sprites for Assurance, showing buildings, obstacles, catwalks, and more (will replace any existing extra radar objects).");
            VowSprites = base.Config.Bind("Radar Sprites", "Vow", true, "Adds new radar sprites for Vow, showing buildings, obstacles, catwalks, and more (will replace any existing extra radar objects).");
            MarchSprites = base.Config.Bind("Radar Sprites", "March", true, "Adds new radar sprites for March, showing buildings, obstacles, catwalks, and more (will replace any existing extra radar objects).");
            RendSprites = base.Config.Bind("Radar Sprites", "Rend", true, "Adds new radar sprites for Rend, showing buildings, obstacles, catwalks, and more (will replace any existing extra radar objects).");
            DineSprites = base.Config.Bind("Radar Sprites", "Dine", true, "Adds new radar sprites for Dine, showing buildings, obstacles, catwalks, and more (will replace any existing extra radar objects).");
            OffenseSprites = base.Config.Bind("Radar Sprites", "Offense", true, "Adds new radar sprites for Offense, showing buildings, obstacles, catwalks, and more (will replace any existing extra radar objects).");
            TitanSprites = base.Config.Bind("Radar Sprites", "Titan", true, "Adds new radar sprites for Titan, showing buildings, obstacles, catwalks, and more (will replace any existing extra radar objects).");
            ArtificeSprites = base.Config.Bind("Radar Sprites", "Artifice", true, "Adds new radar sprites for Artifice, showing buildings, obstacles, catwalks, and more (will replace any existing extra radar objects).");
            AdamanceSprites = base.Config.Bind("Radar Sprites", "Adamance", true, "Adds new radar sprites for Adamance, showing buildings, obstacles, catwalks, and more (will replace any existing extra radar objects).");
            EmbrionSprites = base.Config.Bind("Radar Sprites", "Embrion", true, "Adds new radar sprites for Embrion, showing buildings, obstacles, catwalks, and more (will replace any existing extra radar objects).");
            WaterSprites = base.Config.Bind("Radar Sprites", "Water Textures", true, "Makes bodies of water visible on the radar (affects Vow, March, and Adamance).");

            CameraClipExtension = base.Config.Bind("Camera", "Increased Render Distance", 20f, new ConfigDescription("For the 3D contours generated by this mod to be captured, the region of space captured by the camera needs to be extended as well. Increasing this means more objects/scenery above and below a player will be picked up on the radar screen (set this to 0 for vanilla clipping distance).", new AcceptableValueRange<float>(0f, 30f)));

            batbyPresent = Chainloader.PluginInfos.ContainsKey("imabatby.lethallevelloader");

            string sAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            URAssets = AssetBundle.LoadFromFile(Path.Combine(sAssemblyLocation, "sburassets"));

            if (ExperimentationSprites.Value)
                radarSpritePrefabs.Add("Level1Experimentation", (GameObject)URAssets.LoadAsset("ExperimentationRadarSprites"));
            if (AssuranceSprites.Value)
                radarSpritePrefabs.Add("Level2Assurance", (GameObject)URAssets.LoadAsset("AssuranceRadarSprites"));
            if (VowSprites.Value)
                radarSpritePrefabs.Add("Level3Vow", (GameObject)URAssets.LoadAsset("VowRadarSprites"));
            if (MarchSprites.Value)
                radarSpritePrefabs.Add("Level4March", (GameObject)URAssets.LoadAsset("MarchRadarSprites"));
            if (RendSprites.Value)
                radarSpritePrefabs.Add("Level5Rend", (GameObject)URAssets.LoadAsset("RendRadarSprites"));
            if (DineSprites.Value)
                radarSpritePrefabs.Add("Level6Dine", (GameObject)URAssets.LoadAsset("DineRadarSprites"));
            if (OffenseSprites.Value)
                radarSpritePrefabs.Add("Level7Offense", (GameObject)URAssets.LoadAsset("OffenseRadarSprites"));
            if (TitanSprites.Value)
                radarSpritePrefabs.Add("Level8Titan", (GameObject)URAssets.LoadAsset("TitanRadarSprites"));
            if (ArtificeSprites.Value)
                radarSpritePrefabs.Add("Level9Artifice", (GameObject)URAssets.LoadAsset("ArtificeRadarSprites"));
            if (AdamanceSprites.Value)
                radarSpritePrefabs.Add("Level10Adamance", (GameObject)URAssets.LoadAsset("AdamanceRadarSprites"));
            if (EmbrionSprites.Value)
                radarSpritePrefabs.Add("Level11Embrion", (GameObject)URAssets.LoadAsset("EmbrionRadarSprites"));

            Patch();

            Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
        }

        internal static void Patch()
        {
            Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);

            Logger.LogDebug("Patching...");

            Harmony.PatchAll();

            Logger.LogDebug("Finished patching!");
        }

        internal static void Unpatch()
        {
            Logger.LogDebug("Unpatching...");

            Harmony?.UnpatchSelf();

            Logger.LogDebug("Finished unpatching!");
        }
    }


}
