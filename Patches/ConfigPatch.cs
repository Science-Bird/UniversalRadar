using System.Collections.Generic;
using HarmonyLib;
using BepInEx.Configuration;

namespace UniversalRadar.Patches
{
    [HarmonyPatch]
    public class ConfigPatch
    {
        public static List<(MaterialPropertiesConfig, (string, string))> vanillaConfigs = new List<(MaterialPropertiesConfig,(string,string))>();
        public static List<(string,string)> moonBlacklist = new List<(string,string)>();

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Start))]
        [HarmonyPrefix]
        [HarmonyAfter("imabatby.lethallevelloader")]
        public static void OnStartInitialize()
        {
            // add vanilla config entries (these will eventually all be manual with custom values)
            vanillaConfigs.Add((new MaterialPropertiesConfig("41 Experimentation", "Vanilla", "Auto", 2.5f, 3f, -20f, 50f, 1f, "#4D6A46", "#4D6A46"), ("41 Experimentation", "Level1Experimentation")));
            vanillaConfigs.Add((new MaterialPropertiesConfig("220 Assurance", "Vanilla", "Auto", 2.5f, 3f, -20f, 50f, 1f, "#4D6A46", "#4D6A46"), ("220 Assurance", "Level2Assurance")));
            vanillaConfigs.Add((new MaterialPropertiesConfig("56 Vow", "Vanilla", "Auto", 2.5f, 3f, -4f, 27.5f, 0.8f, "#4D6A46", "#4D6A46"), ("56 Vow", "Level3Vow")));
            vanillaConfigs.Add((new MaterialPropertiesConfig("61 March", "Vanilla", "Auto", 2.5f, 3f, -20f, 50f, 1f, "#4D6A46", "#4D6A46"), ("61 March", "Level4March")));
            vanillaConfigs.Add((new MaterialPropertiesConfig("85 Rend", "Vanilla", "Auto", 2.5f, 3f, -20f, 50f, 1f, "#4D6A46", "#4D6A46"), ("85 Rend", "Level5Rend")));
            vanillaConfigs.Add((new MaterialPropertiesConfig("7 Dine", "Vanilla", "Auto", 2.5f, 3f, -20f, 50f, 1f, "#4D6A46", "#4D6A46"), ("7 Dine", "Level6Dine")));
            vanillaConfigs.Add((new MaterialPropertiesConfig("21 Offense", "Vanilla", "Auto", 2.5f, 3f, -20f, 50f, 1f, "#4D6A46", "#4D6A46"), ("21 Offense", "Level7Offense")));
            vanillaConfigs.Add((new MaterialPropertiesConfig("8 Titan", "Vanilla", "Auto", 2.5f, 3f, -20f, 50f, 1f, "#4D6A46", "#4D6A46"), ("8 Titan", "Level8Titan")));
            vanillaConfigs.Add((new MaterialPropertiesConfig("68 Artifice", "Vanilla", "Auto", 2.5f, 3f, -20f, 50f, 1f, "#4D6A46", "#4D6A46"), ("68 Artifice", "Level9Artifice")));
            vanillaConfigs.Add((new MaterialPropertiesConfig("20 Adamance", "Vanilla", "Auto", 2.5f, 3f, -20f, 50f, 1f, "#4D6A46", "#4D6A46"), ("20 Adamance", "Level10Adamance")));
            vanillaConfigs.Add((new MaterialPropertiesConfig("5 Embrion", "Vanilla", "Auto", 2.5f, 3f, -20f, 50f, 1f, "#4D6A46", "#4D6A46"), ("5 Embrion", "Level11Embrion")));

            foreach (var config in vanillaConfigs)
            {
                if (config.Item1.mode.Value == "Manual")// save material info to dictionary
                {
                    RadarContourPatches.contourDataDict.Add(config.Item2, new MaterialProperties(config.Item1));
                }
                else if (config.Item1.mode.Value == "Ignore")
                {
                    moonBlacklist.Add(config.Item2);
                }
            }

            if (UniversalRadar.batbyPresent)// equivalent procedure for modded moons
            {
                LLLConfigPatch.OnStartInitialize();
            }

        }
    }

    public class MaterialPropertiesConfig// each time an instance of this is constructed, it binds itself to config values
    {
        public ConfigEntry<string> mode;
        public ConfigEntry<float> lineSpacing;
        public ConfigEntry<float> lineThickness;
        public ConfigEntry<float> minHeight;
        public ConfigEntry<float> maxHeight;
        public ConfigEntry<float> opacityCap;
        public ConfigEntry<string> baseColourHex;
        public ConfigEntry<string> lineColourHex;

        public MaterialPropertiesConfig(string moon, string vanilla, string defaultMode, float spacing, float thickness, float min, float max, float opacity, string colourHexBG, string colourHexLine)
        {
            mode = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moon, defaultMode, new ConfigDescription("'Auto' - Automatically generate a contour map at runtime. 'Manual' - Set values yourself for generating the contour map (after setting this, create a new lobby to refresh config). 'Ignore' - Do not change this moon in any way.", new AcceptableValueList<string>(["Auto", "Manual", "Ignore"])));
            if (mode.Value != "Manual")// only bind material values if in Manual mode, so only moons set to manual will have all their config options revealed (after starting a new lobby)
            {
                return;
            }
            lineSpacing = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moon + " - Line Spacing", spacing, new ConfigDescription("Spacing between lines on the contour map for this moon.", new AcceptableValueRange<float>(0.5f, 6f)));
            lineThickness = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moon + " - Line Thickness", thickness, new ConfigDescription("Thickness of lines on the contour map for this moon.", new AcceptableValueRange<float>(0.5f, 8f)));
            minHeight = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moon + " - Shading Minimum", min, new ConfigDescription("Minimum height for contour shading (height where darkest shade starts).", new AcceptableValueRange<float>(-500f, 500f)));
            maxHeight = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moon + " - Shading Maximum", max, new ConfigDescription("Maximum height for contour shading (height where the shade becomes lightest).", new AcceptableValueRange<float>(-500f, 500f)));
            opacityCap = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moon + " - Maximum Opacity", opacity, new ConfigDescription("Maximum opacity of contour shading for this moon (how light the tallest parts of the contour map will be).", new AcceptableValueRange<float>(0.1f, 1f)));
            baseColourHex = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moon + " - Shading Colour Hex Code", colourHexBG, "Colour of the contour shading for this moon (hexadecimal colour code).");
            lineColourHex = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moon + " - Line Colour Hex Code", colourHexLine, "Colour of the contour lines for this moon (hexadecimal colour code)."); ;
        }
    }
}
