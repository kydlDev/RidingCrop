using BepInEx.Configuration;
using KydlCore.Common;
using KydlCore.Core;
using RidingCrop.Presentation.Hub.Registration;
using RidingCrop.Core;

namespace RidingCrop.Bootstrap;

/// <summary>Composition root: the mod's KY hub page (title + settings tab) and its features in the FeatureRegistry (MIGRATION_DECISIONS A4).</summary>
internal sealed class ModInstaller
{
    private readonly ConfigFile _config;

    internal ModInstaller(ConfigFile config)
    {
        _config = config;
        Features = new FeatureRegistry(line => ModLog.Verbose(line));
    }

    internal static ModInstaller Current { get; private set; }

    internal FeatureRegistry Features { get; }

    internal bool Install()
    {
        Current = this;
        bool page = HubTabs.RegisterModPage(_config);
        bool added = true;
        foreach (IFeature feature in new IFeature[] { new CropsFeature(), new SoundsFeature() })
        {
            OperationResponse<int> add = Features.Add(feature);
            if (!add.IsSuccess)
            {
                added = false;
                ModLog.Error("ModInstaller: " + add.Reason);
            }
        }

        { var returnedAt39 = page && added && Refresh(); if (RuleLog.IsOn()) ModLog.Verbose($"ModInstaller.Install: returns {returnedAt39} (line 39)"); return returnedAt39; }
    }

    internal bool Refresh()
    {
        OperationResponse<int> r = Features.Refresh();
        if (r.Status == ResponseStatus.Failed)
        {
            ModLog.Warn("ModInstaller: " + r.Reason);
        }
        else
        {
            ModLog.Verbose("ModInstaller: refresh - " + r);
        }

        { var returnedAt54 = r.IsSuccess; if (RuleLog.IsOn()) ModLog.Verbose($"ModInstaller.Refresh: returns {returnedAt54} (line 54)"); return returnedAt54; }
    }

    internal bool Uninstall()
    {
        OperationResponse<int> r = Features.StopAll();
        bool gone = HubTabs.UnregisterModPage();
        if (Current == this)
        {
            Current = null;
        }

        { var returnedAt67 = r.IsSuccess && gone; if (RuleLog.IsOn()) ModLog.Verbose($"ModInstaller.Uninstall: returns {returnedAt67} (line 67)"); return returnedAt67; }
    }
}
