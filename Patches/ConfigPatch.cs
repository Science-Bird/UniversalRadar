using System.Collections.Generic;
using HarmonyLib;
using BepInEx.Configuration;
using Unity.Burst;
using UnityEngine;

namespace UniversalRadar.Patches
{
    [HarmonyPatch]
    public class ConfigPatch
    {
        public static List<(MaterialPropertiesConfig, (string, string))> vanillaConfigs = new List<(MaterialPropertiesConfig,(string,string))>();
        public static List<(string,string)> moonBlacklist = new List<(string,string)>();
        public static Dictionary<string,string> vanillaSceneDict = new Dictionary<string, string>();

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Start))]
        [HarmonyPrefix]
        [HarmonyAfter(["imabatby.lethallevelloader", "dopadream.lethalcompany.rebalancedmoons"])]
        public static void OnStartInitialize()
        {
            vanillaSceneDict.Clear();
            // dictionary for matching vanilla level names to scene names, used for RebalancedMoon scene checks and to see if a moon is vanilla
            vanillaSceneDict.Add("41 Experimentation", "Level1Experimentation");
            vanillaSceneDict.Add("220 Assurance", "Level2Assurance");
            vanillaSceneDict.Add("56 Vow", "Level3Vow");
            vanillaSceneDict.Add("61 March", "Level4March");
            vanillaSceneDict.Add("85 Rend", "Level5Rend");
            vanillaSceneDict.Add("7 Dine", "Level6Dine");
            vanillaSceneDict.Add("21 Offense", "Level7Offense");
            vanillaSceneDict.Add("8 Titan", "Level8Titan");
            vanillaSceneDict.Add("68 Artifice", "Level9Artifice");
            vanillaSceneDict.Add("20 Adamance", "Level10Adamance");
            vanillaSceneDict.Add("5 Embrion", "Level11Embrion");

            if (UniversalRadar.dopaPresent)// update scene names to patched ones
            {
                LLLConfigPatch.RebalancedMoonsPatch();
            }

            UniversalRadar.radarSpritePrefabs.Clear();
            if (UniversalRadar.ExperimentationSprites.Value)
                UniversalRadar.radarSpritePrefabs.Add(vanillaSceneDict["41 Experimentation"], (GameObject)UniversalRadar.URAssets.LoadAsset("ExperimentationRadarSprites"));
            if (UniversalRadar.AssuranceSprites.Value)
                UniversalRadar.radarSpritePrefabs.Add(vanillaSceneDict["220 Assurance"], (GameObject)UniversalRadar.URAssets.LoadAsset("AssuranceRadarSprites"));
            if (UniversalRadar.VowSprites.Value)
                UniversalRadar.radarSpritePrefabs.Add(vanillaSceneDict["56 Vow"], (GameObject)UniversalRadar.URAssets.LoadAsset("VowRadarSprites"));
            if (UniversalRadar.MarchSprites.Value)
                UniversalRadar.radarSpritePrefabs.Add(vanillaSceneDict["61 March"], (GameObject)UniversalRadar.URAssets.LoadAsset("MarchRadarSprites"));
            if (UniversalRadar.RendSprites.Value)
                UniversalRadar.radarSpritePrefabs.Add(vanillaSceneDict["85 Rend"], (GameObject)UniversalRadar.URAssets.LoadAsset("RendRadarSprites"));
            if (UniversalRadar.DineSprites.Value)
                UniversalRadar.radarSpritePrefabs.Add(vanillaSceneDict["7 Dine"], (GameObject)UniversalRadar.URAssets.LoadAsset("DineRadarSprites"));
            if (UniversalRadar.OffenseSprites.Value)
                UniversalRadar.radarSpritePrefabs.Add(vanillaSceneDict["21 Offense"], (GameObject)UniversalRadar.URAssets.LoadAsset("OffenseRadarSprites"));
            if (UniversalRadar.TitanSprites.Value)
                UniversalRadar.radarSpritePrefabs.Add(vanillaSceneDict["8 Titan"], (GameObject)UniversalRadar.URAssets.LoadAsset("TitanRadarSprites"));
            if (UniversalRadar.ArtificeSprites.Value)
                UniversalRadar.radarSpritePrefabs.Add(vanillaSceneDict["68 Artifice"], (GameObject)UniversalRadar.URAssets.LoadAsset("ArtificeRadarSprites"));
            if (UniversalRadar.AdamanceSprites.Value)
                UniversalRadar.radarSpritePrefabs.Add(vanillaSceneDict["20 Adamance"], (GameObject)UniversalRadar.URAssets.LoadAsset("AdamanceRadarSprites"));
            if (UniversalRadar.EmbrionSprites.Value)
                UniversalRadar.radarSpritePrefabs.Add(vanillaSceneDict["5 Embrion"], (GameObject)UniversalRadar.URAssets.LoadAsset("EmbrionRadarSprites"));


            // add vanilla config entries
            vanillaConfigs.Clear();
            vanillaConfigs.Add((new MaterialPropertiesConfig("41 Experimentation", "Vanilla", defaultMode: "Auto", show: false, lowOpacity: false, extend: false, multiplier: 2f, colourHexBG: "4D6A46", colourHexLine: "4D6A46", spacing: 2.5f, thickness: 6f, min: -10f, max: 30f, opacity: 0.9f), ("41 Experimentation", vanillaSceneDict["41 Experimentation"])));
            vanillaConfigs.Add((new MaterialPropertiesConfig("220 Assurance", "Vanilla", defaultMode: "Auto", show: false, lowOpacity: false, extend: false, multiplier: 2f, colourHexBG: "4D6A46", colourHexLine: "4D6A46", spacing: 2.5f, thickness: 6f, min: -10f, max: 30f, opacity: 0.9f), ("220 Assurance", vanillaSceneDict["220 Assurance"])));
            vanillaConfigs.Add((new MaterialPropertiesConfig("56 Vow", "Vanilla", defaultMode: "Manual", show: false, lowOpacity: false, extend: false, multiplier: 2f, colourHexBG: "4D6A46", colourHexLine: "4D6A46", spacing: 2.5f, thickness: 6f, min: -20f, max: 20f, opacity: 0.9f), ("56 Vow", vanillaSceneDict["56 Vow"])));
            vanillaConfigs.Add((new MaterialPropertiesConfig("61 March", "Vanilla", defaultMode: "Auto", show: false,lowOpacity: false, extend: false, multiplier: 2f, colourHexBG: "4D6A46", colourHexLine: "4D6A46", spacing: 2.5f, thickness: 6f, min: -10f, max: 30f, opacity: 0.9f), ("61 March", vanillaSceneDict["61 March"])));
            vanillaConfigs.Add((new MaterialPropertiesConfig("85 Rend", "Vanilla", defaultMode: "Auto", show: false, lowOpacity: false, extend: false, multiplier: 2f, colourHexBG: "4D6A46", colourHexLine: "4D6A46", spacing: 2.5f, thickness: 6f, min: -10f, max: 30f, opacity: 0.9f), ("85 Rend", vanillaSceneDict["85 Rend"])));
            vanillaConfigs.Add((new MaterialPropertiesConfig("7 Dine", "Vanilla", defaultMode: "Manual", show: false,lowOpacity: false, extend: false, multiplier: 2f, colourHexBG: "4D6A46", colourHexLine: "4D6A46", spacing: 2.5f, thickness: 6f, min: -27f, max: 17f, opacity: 0.9f), ("7 Dine", vanillaSceneDict["7 Dine"])));
            vanillaConfigs.Add((new MaterialPropertiesConfig("21 Offense", "Vanilla", defaultMode: "Auto", show: false, lowOpacity: false, extend: false, multiplier: 2f, colourHexBG: "4D6A46", colourHexLine: "4D6A46", spacing: 2.5f, thickness: 6f, min: -10f, max: 30f, opacity: 0.9f), ("21 Offense", vanillaSceneDict["21 Offense"])));
            vanillaConfigs.Add((new MaterialPropertiesConfig("8 Titan", "Vanilla", defaultMode: "Auto", show: false, lowOpacity: false, extend: true, multiplier: 0.75f, colourHexBG: "4D6A46", colourHexLine: "4D6A46", spacing: 2.5f, thickness: 6f, min: -10f, max: 30f, opacity: 0.9f), ("8 Titan", vanillaSceneDict["8 Titan"])));
            vanillaConfigs.Add((new MaterialPropertiesConfig("68 Artifice", "Vanilla", defaultMode: "Auto", show: false, lowOpacity: false, extend: false, multiplier: 2f, colourHexBG: "4D6A46", colourHexLine: "4D6A46", spacing: 2.5f, thickness: 6f, min: -10f, max: 30f, opacity: 0.9f), ("68 Artifice", vanillaSceneDict["68 Artifice"])));
            vanillaConfigs.Add((new MaterialPropertiesConfig("20 Adamance", "Vanilla", defaultMode: "Manual", show: false, lowOpacity: false, extend: false, multiplier: 2f, colourHexBG: "4D6A46", colourHexLine: "4D6A46", spacing: 2.5f, thickness: 6f, min: -30f, max: 23f, opacity: 0.9f), ("20 Adamance", vanillaSceneDict["20 Adamance"])));
            vanillaConfigs.Add((new MaterialPropertiesConfig("5 Embrion", "Vanilla", defaultMode: "Auto", show: false, lowOpacity: false, extend: false, multiplier: 2f, colourHexBG: "4D6A46", colourHexLine: "4D6A46", spacing: 2.5f, thickness: 6f, min: -10f, max: 30f, opacity: 0.9f), ("5 Embrion", vanillaSceneDict["5 Embrion"])));

            //RadarContourPatches.contourDataDict.Clear();
            foreach (var config in vanillaConfigs)
            {
                // if in manual mode or in auto mode with non-default config
                if (config.Item1.mode.Value == "Manual" || (config.Item1.mode.Value == "Auto" && (config.Item1.extendHeight.Value || !config.Item1.showObjects.Value || config.Item1.lowObjectOpacity.Value || config.Item1.baseColourHex.Value != "4D6A46" || config.Item1.opacityMult.Value != 2f)))
                {
                    if (!RadarContourPatches.contourDataDict.ContainsKey(config.Item2))
                    {
                        RadarContourPatches.contourDataDict.Add(config.Item2, new MaterialProperties(config.Item1));
                    }
                }
                else if (config.Item1.mode.Value == "Ignore")
                {
                    if (!moonBlacklist.Contains(config.Item2))
                    {
                        moonBlacklist.Add(config.Item2);
                    }
                }
            }

            if (UniversalRadar.batbyPresent)// equivalent procedure for modded moons
            {
                LLLConfigPatch.OnStartInitialize();
            }

        }
    }

    // summary of the material properties system:

    // in manual mode, all properties (expect one exclusive to auto) should be bound and used to completely replace auto-generated values (the first time a moon is loaded, all that has to be done is find terrains/make meshes, no height calculations)
    // this means making an entry in contourDataDict, which holds all material-related values for each moon. if an entry doesnt exist already, it will be created when a moon is first loaded, and an entry is only added if certain config is set (e.g. manual mode turned on)
    // entries in this dictionary are pulled on moon load to determine some values
  
    // in auto mode, only a select few properties are present (height properties are exclusive to manual mode, the 2 colour properties are merged into 1 for auto, and settings like spacing, thickness, and max opacity are controlled by the global automatic settings, rather than per moon)
    // this includes one setting exclusive to auto which changes the way automatic height calculations occur (extendHeight)
    // changing these properties will still create an entry in contourDataDict, but when the "auto" flag is detected, it will still run the full calculation code anyways, only using the auto values when the final properties are determined
    // once a moon has been loaded once, this special contourDataDict entry is overwritten with a proper non-auto one (still carrying over whatever values were set, plus the others automatically filled in)

    // when set to ignore, no values are bound at all (except the mode itself)


    public class MaterialPropertiesConfig// each time an instance of this is constructed, it binds itself to config values
    {
        public ConfigEntry<string> mode;
        public ConfigEntry<bool> showObjects;
        public ConfigEntry<bool> lowObjectOpacity;
        public ConfigEntry<bool> extendHeight;
        public ConfigEntry<float> lineSpacing;
        public ConfigEntry<float> lineThickness;
        public ConfigEntry<float> minHeight;
        public ConfigEntry<float> maxHeight;
        public ConfigEntry<float> opacityCap;
        public ConfigEntry<float> opacityMult;
        public ConfigEntry<string> baseColourHex;
        public ConfigEntry<string> lineColourHex;

        public MaterialPropertiesConfig(string moon, string vanilla, string defaultMode, bool show, bool lowOpacity, bool extend, float multiplier, string colourHexBG, string colourHexLine, float spacing, float thickness, float min, float max, float opacity)
        {
            mode = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moon, defaultMode, new ConfigDescription("'Auto' - Automatically generate a contour map at runtime. 'Manual' - Set values yourself for generating the contour map (after setting this, create a new lobby to refresh config). 'Ignore' - Do not change this moon in any way.", new AcceptableValueList<string>(["Auto", "Manual", "Ignore"])));
            if (mode.Value == "Auto")
            {
                showObjects = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moon + " - Show Radar Objects", show, "In addition to creating a terrain contour map, some objects on the map will be rendered on the radar screen as well.");
                lowObjectOpacity = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moon + " - More Translucent Radar Objects", lowOpacity, "Automatically generated radar objects from the above option will display with more transparency. This is recommended when a moon features extensive navigable structures that might normally make excessively bright layered radar sprites.");
                extendHeight = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moon + " - Broader Height Range", extend, "When automatically determining this moon's height range for shading, it will cover a large range of heights than normal (try enabling this if contour shading on a moon becomes too bright too quickly).");
                opacityMult = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moon + " - Opacity Multiplier", multiplier, new ConfigDescription("Opacity multiplier of the shading on this moon's contour map (all shading levels will be multiplied by this number, set higher to make shading generally lighter/higher contrast)..", new AcceptableValueRange<float>(0.1f, 5f)));
                baseColourHex = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moon + " - Colour Hex Code", colourHexBG, "Colour of the contour lines and shading for this moon (hexadecimal colour code).");
            }
            else if (mode.Value == "Manual")// only bind these values if in Manual mode, so only moons set to manual will have all their config options revealed (after starting a new lobby)
            {
                showObjects = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moon + " - Show Radar Objects", show, "In addition to creating a terrain contour map, some objects on the map will be rendered on the radar screen as well.");
                lowObjectOpacity = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moon + " - More Translucent Radar Objects", lowOpacity, "Automatically generated radar objects from the above option will display with more transparency. This is recommended when a moon features extensive navigable structures that might normally make excessively bright layered radar sprites.");
                lineSpacing = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moon + " - Line Spacing", spacing, new ConfigDescription("Spacing between lines on the contour map for this moon.", new AcceptableValueRange<float>(0.5f, 6f)));
                lineThickness = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moon + " - Line Thickness", thickness, new ConfigDescription("Thickness of lines on the contour map for this moon.", new AcceptableValueRange<float>(0.5f, 8f)));
                minHeight = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moon + " - Shading Minimum", min, new ConfigDescription("Minimum height for contour shading (height where darkest shade starts).", new AcceptableValueRange<float>(-500f, 500f)));
                maxHeight = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moon + " - Shading Maximum", max, new ConfigDescription("Maximum height for contour shading (height where the shade becomes lightest).", new AcceptableValueRange<float>(-500f, 500f)));
                opacityCap = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moon + " - Maximum Opacity", opacity, new ConfigDescription("Maximum opacity of contour shading for this moon (how light the tallest parts of the contour map will be).", new AcceptableValueRange<float>(0.1f, 1f)));
                opacityMult = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moon + " - Opacity Multiplier", multiplier, new ConfigDescription("Opacity multiplier of the shading on this moon's contour map (all shading levels will be multiplied by this number, set higher to make shading generally lighter/higher contrast)..", new AcceptableValueRange<float>(0.1f, 5f)));
                baseColourHex = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moon + " - Shading Colour Hex Code", colourHexBG, "Colour of the contour shading for this moon (hexadecimal colour code).");
                lineColourHex = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moon + " - Line Colour Hex Code", colourHexLine, "Colour of the contour lines for this moon (hexadecimal colour code)."); ;
            }
        }
    }
}
