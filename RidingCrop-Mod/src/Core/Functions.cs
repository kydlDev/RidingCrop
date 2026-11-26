using RidingCrop.Core;
namespace RidingCrop;

/// <summary>Pure rules that need no game types (unit-tested).</summary>
internal static class Functions
{
    /// <summary>A functionality is on only when [General] Enable Mod and its own [Section] Enable are both on.</summary>
    /// <param name="enableAll">The global switch.</param>
    /// <param name="own">The functionality's own switch.</param>
    /// <returns>True when the functionality should run.</returns>
    internal static bool IsOn(bool enableAll, bool own)
    {
        if (RuleLog.IsOn()) RuleLog.Verbose($"Functions.IsOn(enableAll={enableAll}, own={own})");
        { var returnedAt14 = enableAll && own; if (RuleLog.IsOn()) RuleLog.Verbose($"Functions.IsOn: returns {returnedAt14} (line 14)"); return returnedAt14; }
    }
}
