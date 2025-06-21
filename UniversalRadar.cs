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
    [BepInDependency("dopadream.lethalcompany.rebalancedmoons", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("MapImprovements", BepInDependency.DependencyFlags.SoftDependency)]
    public class UniversalRadar : BaseUnityPlugin
    {
        public static UniversalRadar Instance { get; private set; } = null!;
        internal new static ManualLogSource Logger { get; private set; } = null!;
        internal static Harmony? Harmony { get; set; }

        public static AssetBundle URAssets;

        public static ConfigEntry<float> AutoLineWidth, AutoSpacing, AutoOpacity;
        public static ConfigEntry<bool> LogValues;
        public static ConfigEntry<bool> UseTerraMesh, HideRadarObjects, RadarWater, ShowFoliage;
        public static ConfigEntry<float> RadarObjectSize;
        public static ConfigEntry<float> CameraClipExtension;
        public static ConfigEntry<bool> ExperimentationSprites, AssuranceSprites, VowSprites, MarchSprites, RendSprites, DineSprites, OffenseSprites, TitanSprites, ArtificeSprites, AdamanceSprites, EmbrionSprites;

        public static Dictionary<string, GameObject> radarSpritePrefabs = new Dictionary<string, GameObject>();

        public static Material contourMaterial;
        public static Material radarFillMat0;
        public static Material radarFillMat1;
        public static Material radarWaterMat;

        public static bool batbyPresent = false;
        public static bool dopaPresent = false;
        public static bool spookyPresent = false;

        private void Awake()
        {
            Logger = base.Logger;
            Instance = this;

            UseTerraMesh = base.Config.Bind("!General", "Convert Terrains To Mesh", true, "When a moon has non-mesh terrain, it will be converted in order to generate the contour map. This will mean a long initial load on maps with non-mesh terrain (this includes most of Wesley's moons), but after the first time, it will store the generated mesh for the rest of the session. If this ends up taking way too long or even causing timeouts/crashes, you can disable it here (though any moons with non-mesh terrain will no longer have contour maps).");
            HideRadarObjects = base.Config.Bind("!General", "Master Disable Radar Objects", false, "Overrides all individual configs and will never attempt to create radar images for non-terrain objects.");
            RadarObjectSize = base.Config.Bind("!General", "Radar Object Display Threshold", 90f, new ConfigDescription("How big objects need to be to render as an object on the radar screen (if that config is enabled on the moon). Make this bigger to display less small objects.", new AcceptableValueRange<float>(50f, 1000f)));
            RadarWater = base.Config.Bind("!General", "Show Water On Radar", true, "Generates a special mesh for water objects on vanilla and modded moons.");
            ShowFoliage = base.Config.Bind("!General", "Show Foliage Radar Objects", false, "Includes meshes on the foliage layer when generating radar objects.");

            AutoSpacing = base.Config.Bind("Automatic Settings", "Line Spacing", 2.5f, new ConfigDescription("Spacing between lines in automatically generated contour maps.", new AcceptableValueRange<float>(0.5f, 6f)));
            AutoLineWidth = base.Config.Bind("Automatic Settings", "Line Thickness", 6f, new ConfigDescription("Thickness of lines in automatically generated contour maps.", new AcceptableValueRange<float>(0.5f, 10f)));
            AutoOpacity = base.Config.Bind("Automatic Settings", "Maximum Opacity", 0.9f, new ConfigDescription("Maximum opacity of the shading on automatically generated contour maps (how light the tallest parts of the contour map will be).", new AcceptableValueRange<float>(0.1f, 1f)));
            LogValues = base.Config.Bind("Automatic Settings", "Log Automatic Values", false, "Logs all contour map values (notably the minimum and maximum height) upon generating on a moon. Use this if you want to know the normally calculated values before manually customizing them yourself (or for other debugging purposes).");

            ExperimentationSprites = base.Config.Bind("Vanilla Radar Sprites", "Experimentation", true, "Adds new radar sprites for Experimentation, showing buildings, obstacles, catwalks, and more (will replace any existing extra radar objects).");
            AssuranceSprites = base.Config.Bind("Vanilla Radar Sprites", "Assurance", true, "Adds new radar sprites for Assurance, showing buildings, obstacles, catwalks, and more (will replace any existing extra radar objects).");
            VowSprites = base.Config.Bind("Vanilla Radar Sprites", "Vow", true, "Adds new radar sprites for Vow, showing buildings, obstacles, catwalks, and more (will replace any existing extra radar objects).");
            MarchSprites = base.Config.Bind("Vanilla Radar Sprites", "March", true, "Adds new radar sprites for March, showing buildings, obstacles, catwalks, and more (will replace any existing extra radar objects).");
            RendSprites = base.Config.Bind("Vanilla Radar Sprites", "Rend", true, "Adds new radar sprites for Rend, showing buildings, obstacles, catwalks, and more (will replace any existing extra radar objects).");
            DineSprites = base.Config.Bind("Vanilla Radar Sprites", "Dine", true, "Adds new radar sprites for Dine, showing buildings, obstacles, catwalks, and more (will replace any existing extra radar objects).");
            OffenseSprites = base.Config.Bind("Vanilla Radar Sprites", "Offense", true, "Adds new radar sprites for Offense, showing buildings, obstacles, catwalks, and more (will replace any existing extra radar objects).");
            TitanSprites = base.Config.Bind("Vanilla Radar Sprites", "Titan", true, "Adds new radar sprites for Titan, showing buildings, obstacles, catwalks, and more (will replace any existing extra radar objects).");
            ArtificeSprites = base.Config.Bind("Vanilla Radar Sprites", "Artifice", true, "Adds new radar sprites for Artifice, showing buildings, obstacles, catwalks, and more (will replace any existing extra radar objects).");
            AdamanceSprites = base.Config.Bind("Vanilla Radar Sprites", "Adamance", true, "Adds new radar sprites for Adamance, showing buildings, obstacles, catwalks, and more (will replace any existing extra radar objects).");
            EmbrionSprites = base.Config.Bind("Vanilla Radar Sprites", "Embrion", true, "Adds new radar sprites for Embrion, showing buildings, obstacles, catwalks, and more (will replace any existing extra radar objects).");

            CameraClipExtension = base.Config.Bind("Camera", "Increased Render Distance", 20f, new ConfigDescription("When outside, the region of space captured by the camera needs to be extended for the 3D contours generated by this mod to be captured. Increasing this means more objects/scenery above and below a player will be picked up on the radar screen (set this to 0 for vanilla clipping distance).", new AcceptableValueRange<float>(0f, 150f)));

            batbyPresent = Chainloader.PluginInfos.ContainsKey("imabatby.lethallevelloader");
            Chainloader.PluginInfos.ContainsKey("dopadream.lethalcompany.rebalancedmoons");

            dopaPresent = Chainloader.PluginInfos.ContainsKey("dopadream.lethalcompany.rebalancedmoons");
            spookyPresent = Chainloader.PluginInfos.ContainsKey("MapImprovements");

            string sAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            URAssets = AssetBundle.LoadFromFile(Path.Combine(sAssemblyLocation, "sburassets"));

            contourMaterial = (Material)URAssets.LoadAsset("ContourMat");
            radarFillMat0 = (Material)URAssets.LoadAsset("RadarGreen0");// regular
            radarFillMat1 = (Material)URAssets.LoadAsset("RadarGreen1");// low opacity
            radarWaterMat = (Material)URAssets.LoadAsset("RadarBlue");// water
            radarWaterMat.renderQueue = 1000;

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
