using KydlCore.Common;
using KydlCore.Core;
using RidingCrop.Core;

namespace RidingCrop;

/// <summary>
/// The seven crops and their skill. Gate: EnableMod and [Crops] Enable. Items and the skill cannot be added or removed while the game runs,
/// so they are registered once during startup; a switch changed later only changes <see cref="Running"/> (the README says: restart).
/// </summary>
internal sealed class CropsFeature : IFeature
{
    internal const string FeatureId = "crops";

    private static bool _registered;

    /// <summary>True between Start and Stop.</summary>
    internal static bool Running { get; private set; }

    public string Id { get { return FeatureId; } }

    public bool IsEnabled() { return ModSettings.CropsOn; }

    public bool Start()
    {
        if (!_registered)
        {
            if (!Plugin.InStartup)
            {
                ModLog.Warn($"CropsFeature: switched on after startup - the crops and the skill are added at the next game start - inStartup={Plugin.InStartup}, registered={_registered}");
                Running = true;
                { if (RuleLog.IsOn()) ModLog.Verbose("CropsFeature.Start: returns true because the game is past startup (InStartup=" + Plugin.InStartup + ", registered=" + _registered + ") (line 32)"); return true; }
            }

            _registered = true;
            if (!CropAssets.Load())
            {
                ModLog.Error($"CropsFeature: the crops were NOT added because the asset bundle could not be loaded - registered={_registered}");
                { if (RuleLog.IsOn()) ModLog.Verbose($"CropsFeature.Start: returns false because the asset bundle could not be loaded (registered={_registered}) (line 39)"); return false; }
            }

            // Skills must be registered before the game creates its Skills object, so do it here and not in the prefab event.
            CropSkill.Register();

            // Vanilla prefabs only exist once the game has loaded them: runs now when they already do, else on the next ObjectDB copy (AddAll is safe to repeat).
            KyPrefabs.WhenVanillaAvailable(CropItems.AddAll);
        }

        Running = true;
        ModLog.Verbose($"CropsFeature: started - running={Running}, registered={_registered}");
        { var returnedAt = Running; if (RuleLog.IsOn()) ModLog.Verbose($"CropsFeature.Start: returns {returnedAt} (line 51)"); return returnedAt; }
    }

    public bool Stop()
    {
        Running = false;
        ModLog.Verbose($"CropsFeature: stopped (items and skill stay until the game restarts) - running={Running}, registered={_registered}");
        { var returnedAt = !Running; if (RuleLog.IsOn()) ModLog.Verbose($"CropsFeature.Stop: returns {returnedAt} (line 58)"); return returnedAt; }
    }
}
