using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    [BepInDependency("TonightWeDine", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("Zaggy1024.TwoRadarMaps", BepInDependency.DependencyFlags.SoftDependency)]
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

        public static ConfigEntry<bool> ClearOrphans;
        public static ConfigEntry<string> ConfigVersion;

        public static Dictionary<string, GameObject> radarSpritePrefabs = new Dictionary<string, GameObject>();

        public static Material contourMaterial;
        public static Material radarFillMat0;
        public static Material radarFillMat1;
        public static Material radarWaterMat;

        public static bool batbyPresent = false;
        public static bool zaggyPresent = false;
        public static bool dopaPresent = false;
        public static bool spookyPresent = false;
        public static bool terraformerPresent = false;

        public static int[] configVersionArray = [-1, -1, -1];
        //public static List<ConfigDefinition> tempConfigs = new List<ConfigDefinition>();

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

            ClearOrphans = base.Config.Bind("!Config Management", "Clear Orphaned Entries On Next Join", false, "This check box acts like a button which will only activate once then go back to being unchecked: after starting a game, all unused config data will be cleared. This means config entries associated with moons that are no longer installed or entries for modes which are no longer active for a given moon (e.g. if you switch a moon's mode from Manual to Automatic, the manual options will still be leftover in the file).");
            //KeepEntries = base.Config.Bind("!Config Management", "Keep Assigned Config Values Before Joining", true, "Normally, since the configs for all the moons are generated when you join/start a game, if you launch and then quit before joining you won't be able to see any of your moon config without looking at the raw config file (effectively they are orphaned). With this enabled, all orphaned config values are temporarily set until a game is joined, so you can still see them and edit them (though this has some limitations, such as descriptions and default values being missing and all orphaned config values being shown). Keeping this enabled might have some impacts on initial launch time.");

            string configVersionString = "-1.-1.-1";
            ConfigDefinition configVersionDef = new ConfigDefinition("!Config Management", "Config Version");

            var orphanedEntriesProperty = base.Config.GetType().GetProperty("OrphanedEntries", BindingFlags.NonPublic | BindingFlags.Instance);
            Dictionary<ConfigDefinition, string> orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProperty!.GetValue(base.Config, null);

            if (orphanedEntries.TryGetValue(configVersionDef, out string version))
            {
                UniversalRadar.Logger.LogDebug($"Found config version: {version} > {MyPluginInfo.PLUGIN_VERSION}");
                configVersionString = version;
            }

            ConfigVersion = base.Config.Bind("!Config Management", "Config Version", "-1.-1.-1", "Used for internal config tracking, please ignore.");

            string[] versionStringNums = configVersionString.Split(".");
            if (versionStringNums.Length != 3 || versionStringNums.Any(x => !int.TryParse(x, out int n)))
            {
                UniversalRadar.Logger.LogWarning("Invalid config version detected!");
                versionStringNums = ["-1", "-1", "-1"];
                ConfigVersion.Value = "-1.-1.-1";
            }
            configVersionArray = Array.ConvertAll(versionStringNums, x => int.Parse(x));

            //if (KeepEntries.Value)
            //{
            //    RestoreOrphans(orphanedEntries);
            //}

            batbyPresent = Chainloader.PluginInfos.ContainsKey("imabatby.lethallevelloader");
            zaggyPresent = Chainloader.PluginInfos.ContainsKey("Zaggy1024.TwoRadarMaps");

            dopaPresent = Chainloader.PluginInfos.ContainsKey("dopadream.lethalcompany.rebalancedmoons");
            spookyPresent = Chainloader.PluginInfos.ContainsKey("MapImprovements");
            terraformerPresent = Chainloader.PluginInfos.ContainsKey("TonightWeDine");

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

        //
        // spent waaay too long on a system to avoid orhpaning everything on boot before I ran into some issues with BepInEx removing entries and had to scrap everything
        //

        //private void RestoreOrphans(Dictionary<ConfigDefinition, string> orphanedEntries)
        //{
        //    List<ConfigDefinition> entryKeys = new List<ConfigDefinition>(orphanedEntries.Keys.Where(x => x.Section == "Moon Overrides - LLL" || x.Section == "Moon Overrides - Vanilla"));
        //    List<ConfigDefinition> modeEntries = new List<ConfigDefinition>(entryKeys.Where(x => !x.Key.Contains(" - ")));
        //    List<string> manualMoons = new List<string>();
        //    List<string> ignoreMoons = new List<string>();


        //    foreach (ConfigDefinition orphan in modeEntries)
        //    {
        //        if (orphanedEntries.TryGetValue(orphan, out string value))
        //        {
        //            //UniversalRadar.Logger.LogInfo($"ORPHAN: {orphan.Section}/{orphan.Key} : {value}");
        //            if (value == "Manual")
        //            {
        //                manualMoons.Add(orphan.Key);
        //            }
        //            else if (value == "Ignore")
        //            {
        //                ignoreMoons.Add(orphan.Key);
        //            }
                    
        //        }
        //    }

        //    foreach (ConfigDefinition orphan in entryKeys)
        //    {
        //        ConfigDefinition configDef = new ConfigDefinition(orphan.Section, orphan.Key);
        //        if (modeEntries.Contains(orphan) && orphanedEntries.TryGetValue(orphan, out string modeValue))
        //        {
        //            base.Config.Bind(orphan.Section, orphan.Key, modeValue, new ConfigDescription("'Auto' - Automatically generate a contour map at runtime. 'Manual' - Set values yourself for generating the contour map (after setting this, create a new lobby to refresh config). 'Ignore' - Do not change this moon in any way (MISSING DEFAULT VALUE).", new AcceptableValueList<string>(["Auto", "Manual", "Ignore"])));
        //            tempConfigs.Add(configDef);
        //        }
        //        else
        //        {
        //            string[] keyData = orphan.Key.Split(" - ");
        //            if (keyData.Length == 2 && !ignoreMoons.Contains(keyData[0]) && ValidWithMode(keyData[1], manualMoons.Contains(keyData[0])) && orphanedEntries.TryGetValue(orphan, out string value))
        //            {
        //                //UniversalRadar.Logger.LogInfo($"ORPHAN: {orphan.Section}/{orphan.Key} : {value}");
        //                if (Boolean.TryParse(value, out bool boolVal))
        //                {
        //                    base.Config.Bind(configDef, false, new ConfigDescription(GetDescription(keyData[1])));
        //                }
        //                else if (float.TryParse(value, out float floatVal))
        //                {
        //                    base.Config.Bind(configDef, -1f, new ConfigDescription(GetDescription(keyData[1])));
        //                }
        //                else
        //                {
        //                    base.Config.Bind(configDef, "missing", new ConfigDescription(GetDescription(keyData[1])));
        //                }
        //                tempConfigs.Add(configDef);
        //            }
        //        }
        //    }
        //}

        //private static bool ValidWithMode(string configKey, bool manual)
        //{
        //    switch (configKey)
        //    {
        //        case "Show Radar Objects":
        //        case "More Translucent Radar Objects":
        //        case "Opacity Multiplier":
        //            return true;
        //        case "Line Spacing":
        //        case "Line Thickness":
        //        case "Shading Minimum":
        //        case "Shading Maximum":
        //        case "Maximum Opacity":
        //        case "Shading Colour Hex Code":
        //        case "Line Colour Hex Code":
        //            return manual;
        //        case "Colour Hex Code":
        //        case "Broader Height Range":
        //            return !manual;
        //        default:
        //            return false;
        //    }
        //}

        //private static string GetDescription(string configKey)
        //{
        //    string description = "";
        //    switch (configKey)
        //    {
        //        case "Show Radar Objects":
        //            description = "In addition to creating a terrain contour map, some objects on the map will be rendered on the radar screen as well.";
        //            break;
        //        case "More Translucent Radar Objects":
        //            description = "Automatically generated radar objects from the above option will display with more transparency. This is recommended when a moon features extensive navigable structures that might normally make excessively bright layered radar sprites.";
        //            break;
        //        case "Line Spacing":
        //            description = "Spacing between lines on the contour map for this moon.";
        //            break;
        //        case "Line Thickness":
        //            description = "Thickness of lines on the contour map for this moon.";
        //            break;
        //        case "Shading Minimum":
        //            description = "Minimum height for contour shading (height where darkest shade starts).";
        //            break;
        //        case "Shading Maximum":
        //            description = "Maximum height for contour shading (height where the shade becomes lightest).";
        //            break;
        //        case "Maximum Opacity":
        //            description = "Maximum opacity of contour shading for this moon (how light the tallest parts of the contour map will be).";
        //            break;
        //        case "Opacity Multiplier":
        //            description = "Opacity multiplier of the shading on this moon's contour map (all shading levels will be multiplied by this number, set higher to make shading generally lighter/higher contrast).";
        //            break;
        //        case "Shading Colour Hex Code":
        //            description = "Colour of the contour shading for this moon (hexadecimal colour code).";
        //            break;
        //        case "Line Colour Hex Code":
        //            description = "Colour of the contour lines for this moon (hexadecimal colour code).";
        //            break;
        //        case "Colour Hex Code":
        //            description = "Colour of the contour lines and shading for this moon (hexadecimal colour code).";
        //            break;
        //        case "Broader Height Range":
        //            description = "When automatically determining this moon's height range for shading, it will cover a large range of heights than normal (try enabling this if contour shading on a moon becomes too bright too quickly).";
        //            break;
        //    }
        //    return description + " (MISSING DEFAULT VALUE)";
        //}

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
