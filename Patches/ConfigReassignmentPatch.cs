using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;

namespace UniversalRadar.Patches
{
    [HarmonyPatch]
    public class ConfigReassignmentPatch
    {

        // simplified version of my config patch for ScienceBird Tweaks
        public static void CheckOrphans()
        {
            ConfigEntryBase[] entryArray = UniversalRadar.Instance.Config.GetConfigEntries();

            var orphanedEntriesProperty = UniversalRadar.Instance.Config.GetType().GetProperty("OrphanedEntries", BindingFlags.NonPublic | BindingFlags.Instance);
            Dictionary<ConfigDefinition, string> orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProperty!.GetValue(UniversalRadar.Instance.Config, null);
            List<ConfigDefinition> orphanKeys = new List<ConfigDefinition>(orphanedEntries.Keys);
            foreach (var entry in orphanKeys)
            {
                if (entry.Section == "Camera" && entry.Key == "Increased Render Distance")
                {
                    if (float.TryParse(orphanedEntries[entry], out float orphanVal) && UniversalRadar.Instance.Config.TryGetEntry<float>(new ConfigDefinition("!General", "Increased Radar Vertical Render Distance"), out var newEntry))
                    {
                        newEntry.Value = orphanVal;
                        orphanedEntries.Remove(entry);
                    }
                }
                else if (entry.Section == "Moon Overrides - Vanilla" || entry.Section == "Moon Overrides - LLL")
                {
                    if (RelocateConfig(entry, "Shading Colour Hex Code", "Moon Overrides (Colour)", "Shading Colour Hex Code", orphanedEntries.GetValueSafe(entry)))
                        orphanedEntries.Remove(entry);
                    else if (RelocateConfig(entry, "Line Colour Hex Code", "Moon Overrides (Colour)", "Line Colour Hex Code", orphanedEntries.GetValueSafe(entry)))
                        orphanedEntries.Remove(entry);
                    else if(RelocateConfig(entry, "Colour Hex Code", "Moon Overrides (Colour)", "Shading Colour Hex Code", orphanedEntries.GetValueSafe(entry)) && RelocateConfig(entry, "Colour Hex Code", "Moon Overrides (Colour)", "Line Colour Hex Code", orphanedEntries.GetValueSafe(entry)))
                        orphanedEntries.Remove(entry);

                    // unlike the others, this isn't to replace an outdated config, but to detect when a moon has been changed to Ignore from either Auto or Manual (Opacity Mult is present in both modes, but not Ignore)
                    // when an orphaned opacity mult linked to an existing Ignore config is found, the radar objects are automatically hidden, then the orphan is removed
                    // the effect of this is when you first set a moon to ignore, it will disable radar objects, but since the orphan is gone after, you're free to set the radar objects however you like after that
                    if (entry.Key.EndsWith("Opacity Multiplier"))
                    {
                        string moonName = entry.Key.Split(" - ")[0];
                        ConfigDefinition configDef = new ConfigDefinition(entry.Section, moonName);
                        if (entryArray.Any(x => x.Definition == configDef) && UniversalRadar.Instance.Config.TryGetEntry<string>(configDef, out var configEntry) && configEntry.Value == "Ignore")
                        {
                            ConfigDefinition nextConfigDef = new ConfigDefinition(entry.Section, $"{moonName} - Show Radar Objects");
                            if (entryArray.Any(x => x.Definition == nextConfigDef) && UniversalRadar.Instance.Config.TryGetEntry<bool>(nextConfigDef, out var configBoolEntry))
                            {
                                configBoolEntry.Value = false;
                                orphanedEntries.Remove(entry);
                            }   
                        }
                    }
                }
            }
            UniversalRadar.Instance.Config.Save();
        }

        static bool RelocateConfig(ConfigDefinition configDef, string name, string newSectionName, string newName, string value)
        {
            string key = configDef.Key;
            string section = configDef.Section;
            if (key.Contains(" - ") && key.EndsWith(name))
            {
                string moonName = key.Split(" - ")[0];
                string vanilla = section.Split(" - ")[1];
                if(UniversalRadar.Instance.Config.TryGetEntry<string>(new ConfigDefinition($"{newSectionName} - {vanilla}", $"{moonName} - {newName}"), out var entry))
                {
                    entry.Value = value;
                    return true;
                }
            }
            return false;
        }
    }
}
