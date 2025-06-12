using System.Collections.Generic;
using BepInEx.Configuration;
using LethalLevelLoader;

namespace UniversalRadar.Patches
{
    public class LLLConfigPatch
    {
        public static List<(MaterialPropertiesConfig, (string, string))> moddedConfigs = new List<(MaterialPropertiesConfig, (string, string))>();

        public static MaterialPropertiesValues GetDefaults((string,string) identifier)// method for overriding stuff, can add edge cases in this mod, or other mods can even patch the method to set certain default values
        {
            // the "mode" property is important here: if set to "Ignore", a moon will be blacklisted and left untouched. if set to "Manual", the given values will be used by default for the moon's contour map.
            // if "Auto" is set, nothing will actually change (all non-vanilla are set to Auto by default), but the values put in will be used if the mode is changed by the user to "Manual"

            if (identifier.Item1 == "115 Wither" && identifier.Item2 == "WitherScene")
            {
                // return new MaterialPropertiesValues(identifier, "Manual", 2.5f, 6f, -10f, 50f, 1f)
            }
            return new MaterialPropertiesValues(identifier);
        }

        public static void OnStartInitialize()
        {
            foreach (var extendedLevel in PatchedContent.ExtendedLevels)
            {
                if (extendedLevel.ContentType != ContentType.Vanilla)// create config entry for all non-vanilla moons
                {
                    MaterialPropertiesValues values = GetDefaults((extendedLevel.SelectableLevel.PlanetName, extendedLevel.SelectableLevel.sceneName));
                    moddedConfigs.Add((new MaterialPropertiesConfig(extendedLevel.SelectableLevel.PlanetName, "LLL", values.mode, values.lineSpacing, values.lineThickness, values.minHeight, values.maxHeight, values.opacityCap, values.baseColourHex, values.lineColourHex), (extendedLevel.SelectableLevel.PlanetName, extendedLevel.SelectableLevel.sceneName)));
                }
            }
            foreach (var config in moddedConfigs)
            {
                if (config.Item1.mode.Value == "Manual")// save material info to dictionary
                {
                    RadarContourPatches.contourDataDict.Add(config.Item2, new MaterialProperties(config.Item1));
                }
                else if (config.Item1.mode.Value == "Ignore")
                {
                    ConfigPatch.moonBlacklist.Add(config.Item2);
                }
            }
        }

        public class MaterialPropertiesValues // this is just for convenience to store all the properties at once to use for the GetDefaults method
        {
            public (string, string) moonIdentifier;
            public string mode;
            public float lineSpacing;
            public float lineThickness;
            public float minHeight;
            public float maxHeight;
            public float opacityCap;
            public string baseColourHex;
            public string lineColourHex;

            public MaterialPropertiesValues((string,string) identifier, string defaultMode = "Auto", float spacing = 2.5f, float thickness = 6f, float min = -20f, float max = 40f, float opacity = 1f, string colourHexBG = "#4D6A46", string colourHexLine = "#4D6A46")
            {
                moonIdentifier = identifier;
                mode = defaultMode;
                lineSpacing = spacing;
                lineThickness = thickness;
                minHeight = min;
                maxHeight = max;
                opacityCap = opacity;
                baseColourHex = colourHexBG;
                lineColourHex = colourHexLine;
            }
        }
    }
}
