using BepInEx.Configuration;
using KydlCore.Common;
using RidingCrop.Core;

namespace RidingCrop.Presentation.Hub.Registration;

/// <summary>What RidingCrop declares to the KY hub (F6, MIGRATION_DECISIONS B11): its title and its settings tab (every config entry).</summary>
internal static class HubTabs
{
    internal const string SettingsId = "settings";

    internal static bool RegisterModPage(ConfigFile config)
    {
        global::KydlCore.Features.Hub.Contracts.KyHub.SetModTitle(Plugin.ModGuid, "Riding Crop");
        var tab = global::KydlCore.Features.Hub.Contracts.KyHub.AddTab(Plugin.ModGuid, SettingsId, "Settings", () => global::KydlCore.Features.Hub.Contracts.KyHub.ConfigEntries(config), 0);
        ModLog.Verbose("HubTabs: settings tab " + (tab != null ? "registered" : "FAILED"));
        return tab != null;
    }

    internal static bool UnregisterModPage()
    {
        global::KydlCore.Features.Hub.Contracts.KyHub.RemoveTab(Plugin.ModGuid, SettingsId);
        { var returnedAt24 = !global::KydlCore.Features.Hub.Contracts.KyHub.HasTab(Plugin.ModGuid, SettingsId); if (RuleLog.IsOn()) ModLog.Verbose($"HubTabs.UnregisterModPage: returns {returnedAt24} (line 24)"); return returnedAt24; }
    }
}
