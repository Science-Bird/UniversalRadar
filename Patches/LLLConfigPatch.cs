using System.Collections.Generic;
using LethalLevelLoader;

namespace UniversalRadar.Patches
{
    public class LLLConfigPatch
    {
        public static List<(MaterialPropertiesConfig, (string, string))> moddedConfigs = new List<(MaterialPropertiesConfig, (string, string))>();
        public static readonly Dictionary<(string, string), MaterialPropertiesValues> changeToIgnore1 = new Dictionary<(string, string), MaterialPropertiesValues>
        {
            {("115 Wither", "WitherScene"),  new MaterialPropertiesValues(defaultMode: "Ignore")},
            {("58 Hyve", "Asteroid14Scene"),  new MaterialPropertiesValues(defaultMode: "Ignore", extend: true, multiplier: 1f)},
            {("48 Desolation", "DesolationScene"),  new MaterialPropertiesValues(defaultMode: "Ignore", extend: true, multiplier: 1f)},
            {("354 Demetrica", "DemetricaScene"),  new MaterialPropertiesValues(defaultMode: "Ignore", extend: true, lowOpacity: true)},
            {("141 Filitrios", "FilitriosScene"),  new MaterialPropertiesValues(defaultMode: "Ignore", lowOpacity: true)},
            {("76 Acidir", "AcidirScene"),  new MaterialPropertiesValues(defaultMode: "Ignore", lowOpacity: true)},
            {("-42 Hyx", "HyxScene"),  new MaterialPropertiesValues(defaultMode: "Ignore", lowOpacity: true)},
            {("46 Infernis", "InfernisScene"),  new MaterialPropertiesValues(defaultMode: "Ignore", lowOpacity: true)},
            {("84 Junic", "JunicScene"),  new MaterialPropertiesValues(defaultMode: "Ignore", lowOpacity: true)},
            {("234 Motra", "MotraScene"),  new MaterialPropertiesValues(defaultMode: "Ignore", lowOpacity: true)},
            {("134 Oldred", "OldredScene"),  new MaterialPropertiesValues(defaultMode: "Ignore", lowOpacity: true)},
            {("132 Trite", "TriteScene"),  new MaterialPropertiesValues(defaultMode: "Ignore", lowOpacity: true)},
            {("67 Utril", "UtrilScene"),  new MaterialPropertiesValues(defaultMode: "Ignore", lowOpacity: true)},
            {("135 Duckstroid-14", "Level135Duckstroid14"),  new MaterialPropertiesValues(defaultMode: "Ignore")},
            {("240 Alcatras", "AlcatrasScene"),  new MaterialPropertiesValues(defaultMode: "Ignore")},
            {("98 Galetry", "MusemaScene"),  new MaterialPropertiesValues(defaultMode: "Ignore")},
            {("57 Asteroid-13", "Asteroid13Scene"),  new MaterialPropertiesValues(defaultMode: "Ignore")},
            {("44 Atlantica", "AtlanticaScene"),  new MaterialPropertiesValues(defaultMode: "Ignore")},
            {("288 Berunah", "BerunahScene"),  new MaterialPropertiesValues(defaultMode: "Ignore")},
            {("235 Calist", "CalistScene"),  new MaterialPropertiesValues(defaultMode: "Ignore")},
            {("842 Core", "CoreScene"),  new MaterialPropertiesValues(defaultMode: "Ignore")},
            {("537 Cubatres", "CubatresScene"),  new MaterialPropertiesValues(defaultMode: "Ignore")},
            {("164 Dreck", "KryteScene"),  new MaterialPropertiesValues(defaultMode: "Ignore")},
            {("555 Empra", "EmpraScene"),  new MaterialPropertiesValues(defaultMode: "Ignore")},
            {("End", "EndScene"),  new MaterialPropertiesValues(defaultMode: "Ignore")},
            {("154 Etern", "EternScene"),  new MaterialPropertiesValues(defaultMode: "Ignore")},
            {("29 Faith", "FaithScene"),  new MaterialPropertiesValues(defaultMode: "Ignore")},
            {("25 Fission-C", "FissionCScene"),  new MaterialPropertiesValues(defaultMode: "Ignore")},
            {("36 Gloom", "GloomScene"),  new MaterialPropertiesValues(defaultMode: "Ignore")},
            {("147 Gratar", "GratarScene"),  new MaterialPropertiesValues(defaultMode: "Ignore")},
            {("4 Thalasso", "HydreneScene"),  new MaterialPropertiesValues(defaultMode: "Ignore")},
            {("35 Lecaro", "LecaroScene"),  new MaterialPropertiesValues(defaultMode: "Ignore")},
            {("94 Polarus", "PolarusScene"),  new MaterialPropertiesValues(defaultMode: "Ignore")},
            {("648 Repress", "RepressScene"),  new MaterialPropertiesValues(defaultMode: "Ignore")},
            {("398 Roart", "RoartScene"),  new MaterialPropertiesValues(defaultMode: "Ignore")}
        };

        public static MaterialPropertiesValues GetDefaults((string,string) identifier, bool extend, bool hide, bool lowOpacity, bool ignore, string baseHex, string lineHex)// method for overriding stuff, can add edge cases in this mod, or other mods can even patch the method to set certain default values
        {
            // the "mode" property is important here:
            // if set to "Ignore", a moon will be blacklisted and left untouched.
            // if set to "Manual", the given values will be used by default for the moon's contour map.
            // if "Auto" is set, a smaller pool of values can be changed (if manual values are changed here, it will just change the default value if a user switches the mode to manual)
            
            // content tag overrides
            if (ignore)
            {
                return new MaterialPropertiesValues(defaultMode: "Ignore");
            }
            if (extend || hide || lowOpacity)
            {
                return new MaterialPropertiesValues(extend: extend, lowOpacity: lowOpacity, show: !hide);
            }

            switch (identifier)// manual overrides
            {
                case ("72 Collateral", "CollateralScene"):
                    return new MaterialPropertiesValues(defaultMode: "Manual", min: -25, max: 10, lowOpacity: true);

                case ("58 Hyve", "Asteroid14Scene"):
                case ("48 Desolation", "DesolationScene"):
                    return new MaterialPropertiesValues(defaultMode: "Ignore", extend: true, multiplier: 1f);
                case ("354 Demetrica", "DemetricaScene"):
                    return new MaterialPropertiesValues(defaultMode: "Ignore", extend: true, lowOpacity: true);
                case ("141 Filitrios", "FilitriosScene"):
                    return new MaterialPropertiesValues(defaultMode: "Ignore", lowOpacity: true);
                case ("76 Acidir", "AcidirScene"):
                case ("-42 Hyx", "HyxScene"):
                case ("46 Infernis", "InfernisScene"):
                case ("84 Junic", "JunicScene"):
                case ("234 Motra", "MotraScene"):
                case ("134 Oldred", "OldredScene"):
                case ("132 Trite", "TriteScene"):
                case ("67 Utril", "UtrilScene"):
                    return new MaterialPropertiesValues(defaultMode: "Ignore", extend: true);
                case ("115 Wither", "WitherScene"):
                case ("135 Duckstroid-14", "Level135Duckstroid14"):
                case ("240 Alcatras", "AlcatrasScene"):
                case ("98 Galetry", "MusemaScene"):
                case ("57 Asteroid-13", "Asteroid13Scene"):
                case ("44 Atlantica", "AtlanticaScene"):
                case ("288 Berunah", "BerunahScene"):
                case ("235 Calist", "CalistScene"):
                case ("842 Core", "CoreScene"):
                case ("42 Cosmocos", "CosmocosScene"):
                case ("537 Cubatres", "CubatresScene"):
                case ("164 Dreck", "KryteScene"):
                case ("555 Empra", "EmpraScene"):
                case ("End", "EndScene"):
                case ("154 Etern", "EternScene"):
                case ("29 Faith", "FaithScene"):
                case ("25 Fission-C", "FissionCScene"):
                case ("36 Gloom", "GloomScene"):
                case ("147 Gratar", "GratarScene"):
                case ("4 Thalasso", "HydreneScene"):
                case ("35 Lecaro", "LecaroScene"):
                case ("94 Polarus", "PolarusScene"):
                case ("648 Repress", "RepressScene"):
                case ("398 Roart", "RoartScene"):
                    return new MaterialPropertiesValues(defaultMode: "Ignore");
                case ("120 Corrosion", "ZenithScene"):
                    return new MaterialPropertiesValues(extend: true, lowOpacity: true);
                case ("32-Rampart", "RampartScene"):
                case ("89 Submersion", "SubmersionScene"):
                    return new MaterialPropertiesValues(lowOpacity: true);
                case ("81 Icebound", "FrozenLakeScene"):
                case ("19 Hydro", "HydroScene"):
                case ("27-Calamitous", "CalamitousScene"):
                case ("33 Pinnacle", "PinnacleLevel"):
                case ("103-Precipice", "Precipice Scene"):
                case ("51-Verdance", "VerdanceScene"):
                    return new MaterialPropertiesValues(extend: true);
                case ("28 Celest", "CelestPlanetScene"):
                case ("12-Boreal", "BorealScene"):
                    return new MaterialPropertiesValues(multiplier: 1f);
            }

            if (baseHex != "N/A" || lineHex != "N/A")
            {
                return new MaterialPropertiesValues(colourHexBG: baseHex != "N/A" ? baseHex : "4D6A46", colourHexLine: lineHex != "N/A" ? lineHex : "4D6A46");
            }

            return new MaterialPropertiesValues();
        }

        public static void OnStartInitialize()
        {
            moddedConfigs.Clear();
            foreach (var extendedLevel in PatchedContent.ExtendedLevels)
            {
                if (extendedLevel.ContentType != ContentType.Vanilla)// create config entry for all non-vanilla moons
                {
                    (string, string) identifier = (extendedLevel.SelectableLevel.PlanetName, extendedLevel.SelectableLevel.sceneName);
                    //UniversalRadar.Logger.LogDebug($"IDENTIFIER: {identifier.Item1}, {identifier.Item2}");

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
                    MaterialPropertiesValues values = GetDefaults(identifier, extend, hide, lowOpacity, ignore, baseColour, lineColour);
                    moddedConfigs.Add((new MaterialPropertiesConfig(extendedLevel.SelectableLevel.PlanetName, "LLL", values), identifier));
                }
            }
            foreach (var config in moddedConfigs)
            {
                // update config values if they're from an outdated config version
                if (ConfigPatch.OlderConfigVersion([1, 1, 0]) && changeToIgnore1.TryGetValue(config.Item2, out MaterialPropertiesValues values))
                {
                    config.Item1.SetValues(config.Item2.Item1, "LLL", values);
                }
                // if in manual mode or in auto mode with non-default config
                if (config.Item1.mode.Value == "Manual" || (config.Item1.mode.Value == "Auto" && (config.Item1.extendHeight.Value || config.Item1.showObjects.Value || config.Item1.lowObjectOpacity.Value || config.Item1.baseColourHex.Value != "4D6A46" || config.Item1.opacityMult.Value != 2f)))// save material info to dictionary
                {
                    if (!RadarContourPatches.contourDataDict.ContainsKey(config.Item2))
                    {
                        RadarContourPatches.contourDataDict.Add(config.Item2, new MaterialProperties(config.Item1));
                    }
                }
                else if (config.Item1.mode.Value == "Ignore")
                {
                    if (!ConfigPatch.moonBlacklist.Contains(config.Item2))
                    {
                        ConfigPatch.moonBlacklist.Add(config.Item2);
                    }
                }
            }
        }

        public static void SceneNamePatch()// update scene names in case they are different (e.g. rebalanced moons)
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

            public MaterialPropertiesValues(string defaultMode = "Auto", bool show = true, bool lowOpacity = false, bool extend = false, float multiplier = 2f, string colourHexBG = "4D6A46", string colourHexLine = "4D6A46", float spacing = 2.5f, float thickness = 6f, float min = -10f, float max = 30f, float opacity = 0.9f)
            {
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
