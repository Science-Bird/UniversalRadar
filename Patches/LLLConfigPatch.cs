using System.Collections.Generic;
using BepInEx.Configuration;
using LethalLevelLoader;

namespace UniversalRadar.Patches
{
    public class LLLConfigPatch
    {
        public static List<(MaterialPropertiesConfig, (string, string))> moddedConfigs = new List<(MaterialPropertiesConfig, (string, string))>();

        public static MaterialPropertiesValues GetDefaults((string,string) identifier, bool extend, bool hide, bool lowOpacity, bool ignore, string baseHex, string lineHex)// method for overriding stuff, can add edge cases in this mod, or other mods can even patch the method to set certain default values
        {
            // the "mode" property is important here:
            // if set to "Ignore", a moon will be blacklisted and left untouched.
            // if set to "Manual", the given values will be used by default for the moon's contour map.
            // if "Auto" is set, a smaller pool of values can be changed (if manual values are changed here, it will just change the default value if a user switches the mode to manual)
            
            // content tag overrides
            if (ignore)
            {
                return new MaterialPropertiesValues(identifier, defaultMode: "Ignore");
            }
            if (extend || hide || lowOpacity)
            {
                return new MaterialPropertiesValues(identifier, extend: extend, lowOpacity: lowOpacity, show: !hide);
            }

            switch (identifier)// manual overrides
            {
                case ("115 Wither", "WitherScene"):
                    return new MaterialPropertiesValues(identifier, defaultMode: "Manual", min: -15, max: 75);
                case ("72 Collateral", "CollateralScene"):
                    return new MaterialPropertiesValues(identifier, defaultMode: "Manual", min: -25, max: 10, lowOpacity: true);
                case ("58 Hyve", "Asteroid14Scene"):
                case ("48 Desolation", "DesolationScene"):
                    return new MaterialPropertiesValues(identifier, extend: true, multiplier: 1f);
                case ("42 Cosmocos", "CosmocosScene"):
                    return new MaterialPropertiesValues(identifier, defaultMode: "Ignore");
                case ("354 Demetrica", "DemetricaScene"):
                case ("120 Corrosion", "ZenithScene"):
                    return new MaterialPropertiesValues(identifier, extend: true, lowOpacity: true);
                case ("141 Filitrios", "FilitriosScene"):
                case ("32-Rampart", "RampartScene"):
                case ("89 Submersion", "SubmersionScene"):
                    return new MaterialPropertiesValues(identifier, lowOpacity: true);
                case ("-42 Hyx", "HyxScene"):
                case ("46 Infernis", "InfernisScene"):
                case ("76 Acidir", "AcidirScene"):
                case ("84 Junic", "JunicScene"):
                case ("234 Motra", "MotraScene"):
                case ("134 Oldred", "OldredScene"):
                case ("132 Trite", "TriteScene"):
                case ("67 Utril", "UtrilScene"):
                case ("81 Icebound", "FrozenLakeScene"):
                case ("19 Hydro", "HydroScene"):
                case ("27-Calamitous", "CalamitousScene"):
                case ("33 Pinnacle", "PinnacleLevel"):
                case ("103-Precipice", "Precipice Scene"):
                case ("51-Verdance", "VerdanceScene"):
                    return new MaterialPropertiesValues(identifier, extend: true);
                case ("28 Celest", "CelestPlanetScene"):
                case ("12-Boreal", "BorealScene"):
                    return new MaterialPropertiesValues(identifier, multiplier: 1f);
            }

            if (baseHex != "N/A" || lineHex != "N/A")
            {
                return new MaterialPropertiesValues(identifier, colourHexBG: baseHex != "N/A" ? baseHex : "4D6A46", colourHexLine: lineHex != "N/A" ? lineHex : "4D6A46");
            }

            return new MaterialPropertiesValues(identifier);
        }

        public static void OnStartInitialize()
        {
            foreach (var extendedLevel in PatchedContent.ExtendedLevels)
            {
                if (extendedLevel.ContentType != ContentType.Vanilla)// create config entry for all non-vanilla moons
                {
                    bool extend = extendedLevel.ContentTags.Exists(x => x.contentTagName == "UniversalRadarExtendHeight");
                    bool hide = extendedLevel.ContentTags.Exists(x => x.contentTagName == "UniversalRadarHideObjects");
                    bool lowOpacity = extendedLevel.ContentTags.Exists(x => x.contentTagName == "UniversalRadarLowOpacityObjects");
                    bool ignore = extendedLevel.ContentTags.Exists(x => x.contentTagName == "UniversalRadarIgnore");

                    string lineColour = "N/A";
                    string baseColour = "N/A";
                    ContentTag lineColourTag = extendedLevel.ContentTags.Find(x => x.contentTagName == "UniversalRadarLineColor");
                    ContentTag baseColourTag = extendedLevel.ContentTags.Find(x => x.contentTagName == "UniversalRadarBaseColor");
                    if (lineColourTag != null)
                    {
                        lineColour = RadarContourPatches.HexFromColour(lineColourTag.contentTagColor);
                    }
                    if (baseColourTag != null)
                    {
                        baseColour = RadarContourPatches.HexFromColour(baseColourTag.contentTagColor);
                    }
                    MaterialPropertiesValues values = GetDefaults((extendedLevel.SelectableLevel.PlanetName, extendedLevel.SelectableLevel.sceneName), extend, hide, lowOpacity, ignore, baseColour, lineColour);
                    moddedConfigs.Add((new MaterialPropertiesConfig(extendedLevel.SelectableLevel.PlanetName, "LLL", defaultMode: values.mode, values.showObjects, values.lowObjectOpacity, values.extendHeight, values.opacityMult, values.baseColourHex, values.lineColourHex, values.lineSpacing, values.lineThickness, values.minHeight, values.maxHeight, values.opacityCap), (extendedLevel.SelectableLevel.PlanetName, extendedLevel.SelectableLevel.sceneName)));
                }
            }
            foreach (var config in moddedConfigs)
            {
                // if in manual mode or in auto mode with non-default config
                if (config.Item1.mode.Value == "Manual" || (config.Item1.mode.Value == "Auto" && (config.Item1.extendHeight.Value || config.Item1.showObjects.Value || config.Item1.lowObjectOpacity.Value || config.Item1.baseColourHex.Value != "4D6A46" || config.Item1.opacityMult.Value != 2f)))// save material info to dictionary
                {
                    RadarContourPatches.contourDataDict.Add(config.Item2, new MaterialProperties(config.Item1));
                }
                else if (config.Item1.mode.Value == "Ignore")
                {
                    ConfigPatch.moonBlacklist.Add(config.Item2);
                }
            }
        }

        public static void RebalancedMoonsPatch()
        {
            foreach (var extendedLevel in PatchedContent.ExtendedLevels)
            {
                if (extendedLevel.ContentType == ContentType.Vanilla && ConfigPatch.vanillaSceneDict.ContainsKey(extendedLevel.SelectableLevel.PlanetName))
                {
                    ConfigPatch.vanillaSceneDict[extendedLevel.SelectableLevel.PlanetName] = extendedLevel.SelectableLevel.sceneName;
                }
            }
        }

        public class MaterialPropertiesValues // this is just for convenience to store all the properties at once to use for the GetDefaults method
        {
            public (string, string) moonIdentifier;
            public string mode;
            public bool showObjects;
            public bool lowObjectOpacity;
            public bool extendHeight;
            public float opacityMult;
            public string baseColourHex;
            public string lineColourHex;
            public float lineSpacing;
            public float lineThickness;
            public float minHeight;
            public float maxHeight;
            public float opacityCap;

            public MaterialPropertiesValues((string, string) identifier, string defaultMode = "Auto", bool show = true, bool lowOpacity = false, bool extend = false, float multiplier = 2f, string colourHexBG = "4D6A46", string colourHexLine = "4D6A46", float spacing = 2.5f, float thickness = 6f, float min = -10f, float max = 30f, float opacity = 0.9f)
            {
                moonIdentifier = identifier;
                mode = defaultMode;
                showObjects = show;
                lowObjectOpacity = lowOpacity;
                extendHeight = extend;
                lineSpacing = spacing;
                lineThickness = thickness;
                minHeight = min;
                maxHeight = max;
                opacityCap = opacity;
                opacityMult = multiplier;
                baseColourHex = colourHexBG;
                lineColourHex = colourHexLine;
            }
        }
    }
}
