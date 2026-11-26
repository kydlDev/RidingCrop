using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using KydlCore.Common;
using RidingCrop.Core;

namespace RidingCrop;

/// <summary>
/// 1.0.0 had [General] Enabled and [General] DebugLogging in the same config file. Their values are carried over once to
/// [General] Enable Mod and [DebugLog] Enable, then the old lines are dropped from the file.
/// </summary>
internal static class ConfigMigration
{
    /// <summary>Imports the old values. Call after <see cref="ModSettings.Bind"/>.</summary>
    /// <param name="config">The plugin's config file.</param>
    internal static void Run(ConfigFile config)
    {
        if (RuleLog.IsOn()) ModLog.Verbose($"ConfigMigration.Run(config={config})");
        bool changed = Import(config, "Enabled", ModSettings.EnableMod);
        changed |= Import(config, "DebugLogging", ModSettings.DebugLog);
        if (changed)
        {
            config.Save();
        }
    }

    // BepInEx keeps the entries of the file that nothing has bound (yet) in this dictionary; it is not part of the public API.
    private static Dictionary<ConfigDefinition, string> Orphans(ConfigFile config)
    {
        if (RuleLog.IsOn()) ModLog.Verbose($"ConfigMigration.Orphans(config={config})");
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        PropertyInfo property = typeof(ConfigFile).GetProperty("OrphanedEntries", Flags);
        if (property != null)
        {
            { var returnedAt36 = property.GetValue(config, null) as Dictionary<ConfigDefinition, string>; if (RuleLog.IsOn()) ModLog.Verbose($"ConfigMigration.Orphans: returns {returnedAt36} (line 36)"); return returnedAt36; }
        }

        FieldInfo field = typeof(ConfigFile).GetField("OrphanedEntries", Flags);
        { var returnedAt40 = field == null ? null : field.GetValue(config) as Dictionary<ConfigDefinition, string>; if (RuleLog.IsOn()) ModLog.Verbose($"ConfigMigration.Orphans: returns {returnedAt40} (line 40)"); return returnedAt40; }
    }

    private static bool Import(ConfigFile config, string oldKey, ConfigEntry<bool> target)
    {
        var def = new ConfigDefinition("General", oldKey);
        Dictionary<ConfigDefinition, string> orphans = Orphans(config);
        string text;
        if (orphans == null || !orphans.TryGetValue(def, out text))
        {
            { if (RuleLog.IsOn()) ModLog.Verbose($"ConfigMigration.Import: returns false (line 50) - inputs (config={config}, oldKey={oldKey}, target={target})"); return false; }
        }

        orphans.Remove(def);
        bool value;
        if (bool.TryParse(text, out value) && target.Value != value)
        {
            target.Value = value;
            ModLog.Info("ConfigMigration: [General] " + oldKey + " = " + value + " carried over to [" + target.Definition.Section + "] " + target.Definition.Key);
        }

        { if (RuleLog.IsOn()) ModLog.Verbose($"ConfigMigration.Import: returns true (line 61) - inputs (config={config}, oldKey={oldKey}, target={target})"); return true; }
    }
}
