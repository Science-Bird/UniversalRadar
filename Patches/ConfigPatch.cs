using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace UniversalRadar.Patches
{
    [HarmonyPatch]
    public class ConfigPatch
    {
        public static List<(MaterialPropertiesConfig, (string, string))> vanillaConfigs = new List<(MaterialPropertiesConfig,(string,string))>();
        public static List<(string,string)> moonBlacklist = new List<(string,string)>();
        public static Dictionary<string,string> vanillaSceneDict = new Dictionary<string, string>();
        public static readonly Vector4 defaultContourGreen = new Vector4(0.3019608f, 0.4156863f, 0.2745098f, 1f);
        public static readonly Vector4 defaultRadarGreen = new Vector4(0.3882353f, 1f, 0.3529412f, 1f);
        public static readonly Vector4 defaultCameraGreen = new Vector4(0f, 0.02745098f, 0.003921569f, 0f);
        public static readonly Vector4 defaultSpriteGreenDark = new Vector4(0.1941176f, 0.5f, 0.1764706f, 1f);
        public static readonly Vector4 defaultSpriteGreenDarker = new Vector4(0.1530435f, 0.4f, 0.1391305f, 1f);
        public static Dictionary<string, (GameObject,string)> radarSpritePrefabs = new Dictionary<string, (GameObject,string)>();
        public static Dictionary<string, (GameObject, string)> fixedRadarSpritePrefabs = new Dictionary<string, (GameObject, string)>();

        [HarmonyPatch(typeof(QuickMenuManager), nameof(QuickMenuManager.Start))]
        [HarmonyPostfix]
        [HarmonyAfter(["imabatby.lethallevelloader", "dopadream.lethalcompany.rebalancedmoons"])]
        public static void OnStartInitialize()
        {
            UniversalRadar.SetTime();
            RadarContourPatches.loaded = false;
            vanillaSceneDict.Clear();
            // dictionary for matching vanilla level names to scene names
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
            vanillaSceneDict.Add("71 Gordion", "CompanyBuilding");

            if (UniversalRadar.batbyPresent)// update scene names in case they are different (e.g. rebalanced moons)
            {
                LLLConfigPatch.SceneNamePatch();
            }

            // add vanilla config entries
            // sprite colours aren't part of the automated config process since they're vanilla only, but intuitively they belong in the vanilla colour overrides section, so they're manually inserted here
            vanillaConfigs.Clear();
            UniversalRadar.ExperimentationSpriteColour = UniversalRadar.Instance.Config.Bind("Moon Overrides (Colour) - Vanilla", "41 Experimentation - Sprite Colour Hex Code", "63FF5A", "The colour of the radar sprites on Experimentation.");
            vanillaConfigs.Add((new MaterialPropertiesConfig("41 Experimentation", "Vanilla", defaultMode: "Auto", show: false, lowOpacity: false, extend: false, multiplier: 2f, spacing: 2.5f, thickness: 6f, min: -10f, max: 30f, opacity: 0.9f, colourProps: new ColourProperties("41 Experimentation", "Vanilla", "4D6A46", "4D6A46", "63FF5A", "000701")), ("41 Experimentation", vanillaSceneDict["41 Experimentation"])));
            UniversalRadar.AssuranceSpriteColour = UniversalRadar.Instance.Config.Bind("Moon Overrides (Colour) - Vanilla", "220 Assurance - Sprite Colour Hex Code", "63FF5A", "The colour of the radar sprites on Assurance.");
            vanillaConfigs.Add((new MaterialPropertiesConfig("220 Assurance", "Vanilla", defaultMode: "Auto", show: false, lowOpacity: false, extend: false, multiplier: 2f, spacing: 2.5f, thickness: 6f, min: -10f, max: 30f, opacity: 0.9f, colourProps: new ColourProperties("220 Assurance", "Vanilla", "4D6A46", "4D6A46", "63FF5A", "000701")), ("220 Assurance", vanillaSceneDict["220 Assurance"])));
            UniversalRadar.VowSpriteColour = UniversalRadar.Instance.Config.Bind("Moon Overrides (Colour) - Vanilla", "56 Vow - Sprite Colour Hex Code", "63FF5A", "The colour of the radar sprites on Vow.");
            vanillaConfigs.Add((new MaterialPropertiesConfig("56 Vow", "Vanilla", defaultMode: "Manual", show: false, lowOpacity: false, extend: false, multiplier: 2f, spacing: 2.5f, thickness: 6f, min: -20f, max: 20f, opacity: 0.9f, colourProps: new ColourProperties("56 Vow", "Vanilla", "4D6A46", "4D6A46", "63FF5A", "000701")), ("56 Vow", vanillaSceneDict["56 Vow"])));
            UniversalRadar.MarchSpriteColour = UniversalRadar.Instance.Config.Bind("Moon Overrides (Colour) - Vanilla", "61 March - Sprite Colour Hex Code", "63FF5A", "The colour of the radar sprites on March.");
            vanillaConfigs.Add((new MaterialPropertiesConfig("61 March", "Vanilla", defaultMode: "Auto", show: false, lowOpacity: false, extend: false, multiplier: 2f, spacing: 2.5f, thickness: 6f, min: -10f, max: 30f, opacity: 0.9f, colourProps: new ColourProperties("61 March", "Vanilla", "4D6A46", "4D6A46", "63FF5A", "000701")), ("61 March", vanillaSceneDict["61 March"])));
            UniversalRadar.RendSpriteColour = UniversalRadar.Instance.Config.Bind("Moon Overrides (Colour) - Vanilla", "85 Rend - Sprite Colour Hex Code", "63FF5A", "The colour of the radar sprites on Rend.");
            vanillaConfigs.Add((new MaterialPropertiesConfig("85 Rend", "Vanilla", defaultMode: "Auto", show: false, lowOpacity: false, extend: false, multiplier: 2f, spacing: 2.5f, thickness: 6f, min: -10f, max: 30f, opacity: 0.9f, colourProps: new ColourProperties("85 Rend", "Vanilla", "4D6A46", "4D6A46", "63FF5A", "000701")), ("85 Rend", vanillaSceneDict["85 Rend"])));
            UniversalRadar.DineSpriteColour = UniversalRadar.Instance.Config.Bind("Moon Overrides (Colour) - Vanilla", "7 Dine - Sprite Colour Hex Code", "63FF5A", "The colour of the radar sprites on Dine.");
            vanillaConfigs.Add((new MaterialPropertiesConfig("7 Dine", "Vanilla", defaultMode: "Manual", show: false, lowOpacity: false, extend: false, multiplier: 2f, spacing: 2.5f, thickness: 6f, min: -27f, max: 17f, opacity: 0.9f, colourProps: new ColourProperties("7 Dine", "Vanilla", "4D6A46", "4D6A46", "63FF5A", "000701")), ("7 Dine", vanillaSceneDict["7 Dine"])));
            UniversalRadar.OffenseSpriteColour = UniversalRadar.Instance.Config.Bind("Moon Overrides (Colour) - Vanilla", "21 Offense - Sprite Colour Hex Code", "63FF5A", "The colour of the radar sprites on Offense.");
            vanillaConfigs.Add((new MaterialPropertiesConfig("21 Offense", "Vanilla", defaultMode: "Auto", show: false, lowOpacity: false, extend: false, multiplier: 2f, spacing: 2.5f, thickness: 6f, min: -10f, max: 30f, opacity: 0.9f, colourProps: new ColourProperties("21 Offense", "Vanilla", "4D6A46", "4D6A46", "63FF5A", "000701")), ("21 Offense", vanillaSceneDict["21 Offense"])));
            UniversalRadar.TitanSpriteColour = UniversalRadar.Instance.Config.Bind("Moon Overrides (Colour) - Vanilla", "8 Titan - Sprite Colour Hex Code", "63FF5A", "The colour of the radar sprites on Titan.");
            vanillaConfigs.Add((new MaterialPropertiesConfig("8 Titan", "Vanilla", defaultMode: "Auto", show: false, lowOpacity: false, extend: true, multiplier: 0.75f, spacing: 2.5f, thickness: 6f, min: -10f, max: 30f, opacity: 0.9f, colourProps: new ColourProperties("8 Titan", "Vanilla", "4D6A46", "4D6A46", "63FF5A", "000701")), ("8 Titan", vanillaSceneDict["8 Titan"])));
            UniversalRadar.ArtificeSpriteColour = UniversalRadar.Instance.Config.Bind("Moon Overrides (Colour) - Vanilla", "68 Artifice - Sprite Colour Hex Code", "63FF5A", "The colour of the radar sprites on Artifice.");
            vanillaConfigs.Add((new MaterialPropertiesConfig("68 Artifice", "Vanilla", defaultMode: "Auto", show: false, lowOpacity: false, extend: false, multiplier: 2f, spacing: 2.5f, thickness: 6f, min: -10f, max: 30f, opacity: 0.9f, colourProps: new ColourProperties("68 Artifice", "Vanilla", "4D6A46", "4D6A46", "63FF5A", "000701")), ("68 Artifice", vanillaSceneDict["68 Artifice"])));
            UniversalRadar.AdamanceSpriteColour = UniversalRadar.Instance.Config.Bind("Moon Overrides (Colour) - Vanilla", "20 Adamance - Sprite Colour Hex Code", "63FF5A", "The colour of the radar sprites on Adamance.");
            vanillaConfigs.Add((new MaterialPropertiesConfig("20 Adamance", "Vanilla", defaultMode: "Manual", show: false, lowOpacity: false, extend: false, multiplier: 2f, spacing: 2.5f, thickness: 6f, min: -30f, max: 23f, opacity: 0.9f, colourProps: new ColourProperties("20 Adamance", "Vanilla", "4D6A46", "4D6A46", "63FF5A", "000701")), ("20 Adamance", vanillaSceneDict["20 Adamance"])));
            UniversalRadar.EmbrionSpriteColour = UniversalRadar.Instance.Config.Bind("Moon Overrides (Colour) - Vanilla", "5 Embrion - Sprite Colour Hex Code", "63FF5A", "The colour of the radar sprites on Embrion.");
            vanillaConfigs.Add((new MaterialPropertiesConfig("5 Embrion", "Vanilla", defaultMode: "Auto", show: false, lowOpacity: false, extend: false, multiplier: 2f, spacing: 2.5f, thickness: 6f, min: -10f, max: 30f, opacity: 0.9f, colourProps: new ColourProperties("5 Embrion", "Vanilla", "4D6A46", "4D6A46", "63FF5A", "000701")), ("5 Embrion", vanillaSceneDict["5 Embrion"])));
            UniversalRadar.GordionSpriteColour = UniversalRadar.Instance.Config.Bind("Moon Overrides (Colour) - Vanilla", "71 Gordion - Sprite Colour Hex Code", "63FF5A", "The colour of the radar sprites on Gordion.");
            vanillaConfigs.Add((new MaterialPropertiesConfig("71 Gordion", "Vanilla", defaultMode: "Ignore", show: false, lowOpacity: false, extend: false, multiplier: 2f, spacing: 2.5f, thickness: 6f, min: -10f, max: 30f, opacity: 0.9f, colourProps: new ColourProperties("71 Gordion", "Vanilla", "4D6A46", "4D6A46", "63FF5A", "000701")), ("71 Gordion", vanillaSceneDict["71 Gordion"])));

            // add radar sprites to dictionary and also change their colour if needed
            radarSpritePrefabs.Clear();
            if (UniversalRadar.ExperimentationSprites.Value)
                RegisterSprites("41 Experimentation", "ExperimentationRadarSprites", UniversalRadar.ExperimentationSpriteColour.Value, true);
            if (UniversalRadar.AssuranceSprites.Value)
                RegisterSprites("220 Assurance", "AssuranceRadarSprites", UniversalRadar.AssuranceSpriteColour.Value, true);
            if (UniversalRadar.VowSprites.Value)
                RegisterSprites("56 Vow", "VowRadarSprites", UniversalRadar.VowSpriteColour.Value);
            if (UniversalRadar.MarchSprites.Value)
                RegisterSprites("61 March", "MarchRadarSprites", UniversalRadar.MarchSpriteColour.Value, true);
            if (UniversalRadar.RendSprites.Value)
                RegisterSprites("85 Rend", "RendRadarSprites", UniversalRadar.RendSpriteColour.Value, true);
            if (UniversalRadar.DineSprites.Value)
                RegisterSprites("7 Dine", "DineRadarSprites", UniversalRadar.DineSpriteColour.Value);
            if (UniversalRadar.OffenseSprites.Value)
                RegisterSprites("21 Offense", "OffenseRadarSprites", UniversalRadar.OffenseSpriteColour.Value, true);
            if (UniversalRadar.TitanSprites.Value)
                RegisterSprites("8 Titan", "TitanRadarSprites", UniversalRadar.TitanSpriteColour.Value, true);
            if (UniversalRadar.ArtificeSprites.Value)
                RegisterSprites("68 Artifice", "ArtificeRadarSprites", UniversalRadar.ArtificeSpriteColour.Value);
            if (UniversalRadar.AdamanceSprites.Value)
                RegisterSprites("20 Adamance", "AdamanceRadarSprites", UniversalRadar.AdamanceSpriteColour.Value);
            if (UniversalRadar.EmbrionSprites.Value)
                RegisterSprites("5 Embrion", "EmbrionRadarSprites", UniversalRadar.EmbrionSpriteColour.Value);
            if (UniversalRadar.GordionSprites.Value)
                RegisterSprites("71 Gordion", "GordionRadarSprites", UniversalRadar.GordionSpriteColour.Value, true, true);

            // Add the config properties as actual properties objects to be used at runtime
            foreach (var config in vanillaConfigs)
            {
                // for modded moons, there's a check to add them into the blacklist if they're set to ignore with no radar objects, but since vanilla moons have sprites they're never added to the blacklist
                if (!RadarContourPatches.contourDataDict.ContainsKey(config.Item2))
                {
                    RadarContourPatches.contourDataDict.Add(config.Item2, new MaterialProperties(config.Item1));
                }
            }

            if (UniversalRadar.batbyPresent)// equivalent procedure for modded moons
            {
                LLLConfigPatch.OnStartInitialize();
            }

            ConfigReassignmentPatch.CheckOrphans();// this checks for certain outdated config values and updates them to the correct entries

            if (UniversalRadar.ClearOrphans.Value)
            {
                var orphanedEntriesProperty = UniversalRadar.Instance.Config.GetType().GetProperty("OrphanedEntries", BindingFlags.NonPublic | BindingFlags.Instance);
                var orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProperty!.GetValue(UniversalRadar.Instance.Config, null);

                orphanedEntries.Clear();
                UniversalRadar.ClearOrphans.Value = false;
            }
            UniversalRadar.ConfigVersion.Value = MyPluginInfo.PLUGIN_VERSION;
            UniversalRadar.Instance.Config.Save();
            UniversalRadar.Logger.LogDebug($"Config processing complete ({UniversalRadar.GetTime()}s)");
        }

        static void RegisterSprites(string moon, string assetName, string colourHex, bool hasStatic = false, bool staticOnly = false)
        {
            if (colourHex != "63FF5A")// if colours need to be changed
            {
                Vector4 spriteColourTest = ColourProperties.ColourFromHex(colourHex);
                if (spriteColourTest.x >= 0f)// valid colour
                {
                    // add with custom colours
                    if (!staticOnly)
                        radarSpritePrefabs.Add(vanillaSceneDict[moon], ((GameObject)UniversalRadar.URAssets.LoadAsset(assetName), colourHex));
                    if (hasStatic)
                        fixedRadarSpritePrefabs.Add(vanillaSceneDict[moon], ((GameObject)UniversalRadar.URAssets.LoadAsset(assetName + "Static"), colourHex));
                    Color spriteColour = (Color)spriteColourTest;
                    Color spriteColourDark = spriteColour.RGBMultiplied(0.5f);
                    Color spriteColourDarker = spriteColour.RGBMultiplied(0.4f);
                    if (!staticOnly)
                    {
                        GameObject spritePrefab = radarSpritePrefabs[vanillaSceneDict[moon]].Item1;
                        SpriteRenderer[] sprites = spritePrefab.GetComponentsInChildren<SpriteRenderer>();
                        for (int i = 0; i < sprites.Length; i++)
                        {
                            if (sprites[i].color == (Color)defaultRadarGreen)
                            {
                                sprites[i].color = spriteColour;
                            }
                            else if (sprites[i].color == (Color)defaultSpriteGreenDark)
                            {
                                sprites[i].color = spriteColourDark;
                            }
                            else if (sprites[i].color == (Color)defaultSpriteGreenDarker)
                            {
                                sprites[i].color = spriteColourDarker;
                            }
                        }
                    }
                    if (hasStatic)
                    {
                        GameObject spritePrefabFixed = fixedRadarSpritePrefabs[vanillaSceneDict[moon]].Item1;
                        SpriteRenderer[] spritesFixed = spritePrefabFixed.GetComponentsInChildren<SpriteRenderer>();
                        for (int i = 0; i < spritesFixed.Length; i++)
                        {
                            if (spritesFixed[i].color == (Color)defaultRadarGreen)
                            {
                                spritesFixed[i].color = spriteColour;
                            }
                            else if (spritesFixed[i].color == (Color)defaultSpriteGreenDark)
                            {
                                spritesFixed[i].color = spriteColourDark;
                            }
                            else if (spritesFixed[i].color == (Color)defaultSpriteGreenDarker)
                            {
                                spritesFixed[i].color = spriteColourDarker;
                            }
                        }
                    }
                    return;
                }
            }
            // add with default colours
            if (!staticOnly)
                radarSpritePrefabs.Add(vanillaSceneDict[moon], ((GameObject)UniversalRadar.URAssets.LoadAsset(assetName), "63FF5A"));
            if (hasStatic)
                fixedRadarSpritePrefabs.Add(vanillaSceneDict[moon], ((GameObject)UniversalRadar.URAssets.LoadAsset(assetName + "Static"), "63FF5A"));
            
        }

        public static bool OlderConfigVersion(int[] targetVersion)// older than but not equal to target version
        {
            if (UniversalRadar.configVersionArray.Length == 3 && targetVersion.Length == 3)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (UniversalRadar.configVersionArray[i] == targetVersion[i])
                    {
                        continue;
                    }
                    if (UniversalRadar.configVersionArray[i] < targetVersion[i])
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return false;
        }
    }



    // summary of the material properties system:

    // in Manual mode, all the exact value properties are configurable, and these get used directly at runtime

    // in Auto mode, there's just the opacity multiplier (also present in Manual) and a unique property which changes the way some values are calculated at runtime
    // since it doesn't have any properties set directly, all the values are either computed at runtime or taken from the overall Auto config used to set some values for all moons in Auto mode

    // in Ignore mode, no contour map is generated, so no values are needed nor calculated at runtime

    // all three modes will have two configs for radar object generation (since these are separate from contour maps, they're just bundled into MaterialProperties for convenience)
    // below this, you'll also see the ColourProperties object, which is just a simplified version for different colour values that doesn't really depend on mode or anything like that, but this ColourProperties object does get bundled into MaterialProperties (again, for convenience)

    // moons with applicable values are added to a dictionary which is used at runtime to generate contour maps and radar objects
    // if a moon is in Auto mode, once its values are computed once, it internally changes to a Manual entry and can just grab the values directly every time (so if a moon is already in Manual, it basically skips this first-time setup)
    // moons in Ignore mode still get checked through if they generate radar objects (or are vanilla with sprites), with all the contour stuff being skipped

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
        public ColourProperties colours;

        // construct from raw values (used for vanilla)
        public MaterialPropertiesConfig(string moon, string vanilla, string defaultMode, bool show, bool lowOpacity, bool extend, float multiplier, float spacing, float thickness, float min, float max, float opacity, ColourProperties colourProps)
        {
            string moonClean = moon.Replace("\n", "").Replace("\t", "").Replace("\\", "").Replace("\"", "").Replace("'", "").Replace("[", "").Replace("]", "");
            mode = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moonClean, defaultMode, new ConfigDescription("'Auto' - Automatically generate a contour map at runtime. 'Manual' - Set values yourself for generating the contour map (after setting this, create a new lobby to refresh config). 'Ignore' - Do not change this moon in any way.", new AcceptableValueList<string>(["Auto", "Manual", "Ignore"])));

            // all modes
            showObjects = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moonClean + " - Show Radar Objects", show, "Some objects on the map will be rendered on the radar screen as well.");
            lowObjectOpacity = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moonClean + " - More Translucent Radar Objects", lowOpacity, "Automatically generated radar objects from the above option will display with more transparency. This is recommended when a moon features extensive navigable structures that might normally make excessively bright layered radar sprites.");
            
            if (mode.Value != "Ignore")// both auto and manual
            {
                opacityMult = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moonClean + " - Opacity Multiplier", multiplier, new ConfigDescription("Opacity multiplier of the shading on this moon's contour map (all shading levels will be multiplied by this number, set higher to make shading generally lighter/higher contrast).", new AcceptableValueRange<float>(0.1f, 5f)));
            }
            if (mode.Value == "Auto")// only auto
            {
                extendHeight = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moonClean + " - Broader Height Range", extend, "When automatically determining this moon's height range for shading, it will cover a large range of heights than normal (try enabling this if contour shading on a moon becomes too bright too quickly).");
            }
            else if (mode.Value == "Manual")// only bind these values if in Manual mode, so only moons set to manual will have all their config options revealed (after starting a new lobby)
            {
                lineSpacing = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moonClean + " - Line Spacing", spacing, new ConfigDescription("Spacing between lines on the contour map for this moon.", new AcceptableValueRange<float>(0.5f, 6f)));
                lineThickness = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moonClean + " - Line Thickness", thickness, new ConfigDescription("Thickness of lines on the contour map for this moon.", new AcceptableValueRange<float>(0.5f, 8f)));
                minHeight = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moonClean + " - Shading Minimum", min, new ConfigDescription("Minimum height for contour shading (height where darkest shade starts).", new AcceptableValueRange<float>(-500f, 500f)));
                maxHeight = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moonClean + " - Shading Maximum", max, new ConfigDescription("Maximum height for contour shading (height where the shade becomes lightest).", new AcceptableValueRange<float>(-500f, 500f)));
                opacityCap = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moonClean + " - Maximum Opacity", opacity, new ConfigDescription("Maximum opacity of contour shading for this moon (how light the tallest parts of the contour map will be).", new AcceptableValueRange<float>(0.1f, 1f)));
            }
            colours = colourProps;
        }

        // construct from a helper object (used for modded)
        public MaterialPropertiesConfig(string moon, string vanilla, LLLConfigPatch.MaterialPropertiesValues propertyValues)
        {
            string moonClean = moon.Replace("\n", "").Replace("\t", "").Replace("\\", "").Replace("\"", "").Replace("'", "").Replace("[", "").Replace("]", "");
            mode = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moonClean, propertyValues.mode, new ConfigDescription("'Auto' - Automatically generate a contour map at runtime. 'Manual' - Set values yourself for generating the contour map (after setting this, create a new lobby to refresh config). 'Ignore' - Do not change this moon in any way.", new AcceptableValueList<string>(["Auto", "Manual", "Ignore"])));

            showObjects = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moonClean + " - Show Radar Objects", propertyValues.showObjects, "Some objects on the map will be rendered on the radar screen as well.");
            lowObjectOpacity = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moonClean + " - More Translucent Radar Objects", propertyValues.lowObjectOpacity, "Automatically generated radar objects from the above option will display with more transparency. This is recommended when a moon features extensive navigable structures that might normally make excessively bright layered radar sprites.");
            
            if (mode.Value != "Ignore")// both auto and manual
            {
                opacityMult = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moonClean + " - Opacity Multiplier", propertyValues.opacityMult, new ConfigDescription("Opacity multiplier of the shading on this moon's contour map (all shading levels will be multiplied by this number, set higher to make shading generally lighter/higher contrast).", new AcceptableValueRange<float>(0.1f, 5f)));
            }
            if (mode.Value == "Auto")// only auto
            {
                extendHeight = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moonClean + " - Broader Height Range", propertyValues.extendHeight, "When automatically determining this moon's height range for shading, it will cover a large range of heights than normal (try enabling this if contour shading on a moon becomes too bright too quickly).");
            }
            else if (mode.Value == "Manual")// only bind these values if in Manual mode, so only moons set to manual will have all their config options revealed (after starting a new lobby)
            {
                lineSpacing = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moonClean + " - Line Spacing", propertyValues.lineSpacing, new ConfigDescription("Spacing between lines on the contour map for this moon.", new AcceptableValueRange<float>(0.5f, 6f)));
                lineThickness = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moonClean + " - Line Thickness", propertyValues.lineThickness, new ConfigDescription("Thickness of lines on the contour map for this moon.", new AcceptableValueRange<float>(0.5f, 8f)));
                minHeight = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moonClean + " - Shading Minimum", propertyValues.minHeight, new ConfigDescription("Minimum height for contour shading (height where darkest shade starts).", new AcceptableValueRange<float>(-500f, 500f)));
                maxHeight = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moonClean + " - Shading Maximum", propertyValues.maxHeight, new ConfigDescription("Maximum height for contour shading (height where the shade becomes lightest).", new AcceptableValueRange<float>(-500f, 500f)));
                opacityCap = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moonClean + " - Maximum Opacity", propertyValues.opacityCap, new ConfigDescription("Maximum opacity of contour shading for this moon (how light the tallest parts of the contour map will be).", new AcceptableValueRange<float>(0.1f, 1f)));
            }
            colours = new ColourProperties(moon, vanilla, propertyValues.baseColourHex, propertyValues.lineColourHex, propertyValues.radarColourHex, propertyValues.bgColourHex);
        }

        // update current config values based on a helper object (used to retroactively change config defaults for modded moons)
        public void SetValues(string moon, string vanilla, LLLConfigPatch.MaterialPropertiesValues propertyValues)// for changing existing config (e.g. overriding an old versions default values)
        {
            string moonClean = moon.Replace("\n", "").Replace("\t", "").Replace("\\", "").Replace("\"", "").Replace("'", "").Replace("[", "").Replace("]", "");
            mode.Value = propertyValues.mode;

            showObjects.Value = propertyValues.showObjects;
            lowObjectOpacity.Value = propertyValues.lowObjectOpacity;

            colours.baseColourHex.Value = propertyValues.baseColourHex;
            colours.lineColourHex.Value = propertyValues.lineColourHex;
            colours.radarColourHex.Value = propertyValues.radarColourHex;
            colours.bgColourHex.Value = propertyValues.bgColourHex;
            colours.SetColours();
           
            if (mode.Value != "Ignore")
            {
                if (opacityMult == null)
                    opacityMult = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moonClean + " - Opacity Multiplier", propertyValues.opacityMult, new ConfigDescription("Opacity multiplier of the shading on this moon's contour map (all shading levels will be multiplied by this number, set higher to make shading generally lighter/higher contrast).", new AcceptableValueRange<float>(0.1f, 5f)));

                opacityMult.Value = propertyValues.opacityMult;
            }
            else
            {
                UniversalRadar.Instance.Config.Remove(new ConfigDefinition("Moon Overrides - " + vanilla, moonClean + " - Opacity Multiplier"));
            }
            if (mode.Value == "Auto")
            {
                if (extendHeight == null)
                    extendHeight = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moonClean + " - Broader Height Range", propertyValues.extendHeight, "When automatically determining this moon's height range for shading, it will cover a large range of heights than normal (try enabling this if contour shading on a moon becomes too bright too quickly).");

                extendHeight.Value = propertyValues.extendHeight;
            }
            else
            {
                UniversalRadar.Instance.Config.Remove(new ConfigDefinition("Moon Overrides - " + vanilla, moonClean + " - Broader Height Range"));
            }
            if (mode.Value == "Manual")
            {
                if (lineSpacing == null)
                    lineSpacing = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moonClean + " - Line Spacing", propertyValues.lineSpacing, new ConfigDescription("Spacing between lines on the contour map for this moon.", new AcceptableValueRange<float>(0.5f, 6f)));
                if (lineThickness == null)
                    lineThickness = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moonClean + " - Line Thickness", propertyValues.lineThickness, new ConfigDescription("Thickness of lines on the contour map for this moon.", new AcceptableValueRange<float>(0.5f, 8f)));
                if (minHeight == null)
                    minHeight = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moonClean + " - Shading Minimum", propertyValues.minHeight, new ConfigDescription("Minimum height for contour shading (height where darkest shade starts).", new AcceptableValueRange<float>(-500f, 500f)));
                if (maxHeight == null)
                    maxHeight = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moonClean + " - Shading Maximum", propertyValues.maxHeight, new ConfigDescription("Maximum height for contour shading (height where the shade becomes lightest).", new AcceptableValueRange<float>(-500f, 500f)));
                if (opacityCap == null)
                    opacityCap = UniversalRadar.Instance.Config.Bind("Moon Overrides - " + vanilla, moonClean + " - Maximum Opacity", propertyValues.opacityCap, new ConfigDescription("Maximum opacity of contour shading for this moon (how light the tallest parts of the contour map will be).", new AcceptableValueRange<float>(0.1f, 1f)));

                lineSpacing.Value = propertyValues.lineSpacing;
                lineThickness.Value = propertyValues.lineThickness;
                minHeight.Value = propertyValues.minHeight;
                maxHeight.Value = propertyValues.maxHeight;
            }
            else
            {
                UniversalRadar.Instance.Config.Remove(new ConfigDefinition("Moon Overrides - " + vanilla, moonClean + " - Line Spacing"));
                UniversalRadar.Instance.Config.Remove(new ConfigDefinition("Moon Overrides - " + vanilla, moonClean + " - Line Thickness"));
                UniversalRadar.Instance.Config.Remove(new ConfigDefinition("Moon Overrides - " + vanilla, moonClean + " - Shading Minimum"));
                UniversalRadar.Instance.Config.Remove(new ConfigDefinition("Moon Overrides - " + vanilla, moonClean + " - Shading Maximum"));
                UniversalRadar.Instance.Config.Remove(new ConfigDefinition("Moon Overrides - " + vanilla, moonClean + " - Maximum Opacity"));
            }
        }
    }

    public class ColourProperties
    {
        public ConfigEntry<string> baseColourHex;
        public ConfigEntry<string> lineColourHex;
        public ConfigEntry<string> radarColourHex;
        public ConfigEntry<string> bgColourHex;
        public Vector4 baseColour;
        public Vector4 lineColour;
        public Vector4 radarColour;
        public Vector4 bgColour;


        public ColourProperties(string moon, string vanilla, string colourHexBase, string colourHexLine, string colourHexRadar, string colourHexBG)
        {
            string moonClean = moon.Replace("\n", "").Replace("\t", "").Replace("\\", "").Replace("\"", "").Replace("'", "").Replace("[", "").Replace("]", "");
            baseColourHex = UniversalRadar.Instance.Config.Bind("Moon Overrides (Colour) - " + vanilla, moonClean + " - Shading Colour Hex Code", colourHexBase, "Colour of the contour shading for this moon (hexadecimal colour code).");
            lineColourHex = UniversalRadar.Instance.Config.Bind("Moon Overrides (Colour) - " + vanilla, moonClean + " - Line Colour Hex Code", colourHexLine, "Colour of the contour lines for this moon (hexadecimal colour code).");
            radarColourHex = UniversalRadar.Instance.Config.Bind("Moon Overrides (Colour) - " + vanilla, moonClean + " - Object Colour Hex Code", colourHexRadar, "Colour of the radar objects generated on this moon (hexadecimal colour code).");
            bgColourHex = UniversalRadar.Instance.Config.Bind("Moon Overrides (Colour) - " + vanilla, moonClean + " - Background Colour Hex Code", colourHexBG, "Colour tint of radar background for this moon (hexadecimal colour code).");
            SetColours();
        }

        public void SetColours()
        {
            baseColour = ConfigPatch.defaultContourGreen;
            Vector4 colourBase = ColourFromHex(baseColourHex.Value);
            if (colourBase.x >= 0f)
            {
                baseColour = colourBase;
            }
            lineColour = ConfigPatch.defaultContourGreen;
            Vector4 colourLine = ColourFromHex(lineColourHex.Value);
            if (colourLine.x >= 0f)
            {
                lineColour = colourLine;
            }
            radarColour = ConfigPatch.defaultRadarGreen;
            Vector4 colourRadar = ColourFromHex(radarColourHex.Value);
            if (colourRadar.x >= 0f)
            {
                radarColour = colourRadar;
            }
            bgColour = ConfigPatch.defaultCameraGreen;
            Vector4 colourBG = ColourFromHex(bgColourHex.Value, true);
            if (colourBG.x >= 0f)
            {
                bgColour = colourBG;
            }
        }

        public static Vector4 ColourFromHex(string hexCode, bool zeroAlpha = false)
        {
            string hex = hexCode.Replace("#", "").ToUpper();
            if (hex.Length == 6 && Regex.Match(hex, "^[A-F0-9]{6}$").Success)
            {
                float R = System.Convert.ToInt32(hex.Substring(0, 2), 16) / 255f;
                float G = System.Convert.ToInt32(hex.Substring(2, 2), 16) / 255f;
                float B = System.Convert.ToInt32(hex.Substring(4, 2), 16) / 255f;
                if (!zeroAlpha)
                {
                    return new Vector4(R, G, B, 1f);
                }
                else
                {
                    return new Vector4(R, G, B, 0f);
                }
            }
            return new Vector4(-1f, -1f, -1f, -1f);
        }

        public static string HexFromColour(Color colour)
        {
            string R = colour.r.ToString("X2");
            string G = colour.g.ToString("X2");
            string B = colour.b.ToString("X2");
            return "#" + R + G + B;
        }
    }
}
