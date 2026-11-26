using System;
using KydlCore.Common;
using UnityEngine;

namespace RidingCrop;

/// <summary>The RidingCrop skill that all crops train (instead of the Clubs skill).</summary>
internal static class CropSkill
{
    // Unique skill id. DO NOT CHANGE after release: it generates the skill's number, and players would lose their progress.
    private const string SkillIdentifier = "com.kydldev.ridingcrop.ridingcrop";

    // Skill name shown in the Skills window. The game's "raiseskill" / "resetskill" console commands match this name
    // case-insensitively, so the dev command is:  raiseskill ridingcrop 50
    // Changing the NAME is safe: saved progress is tied to SkillIdentifier above, not to this text.
    private const string SkillName = "RidingCrop";

    /// <summary>The registered skill type, or None when registering failed (the crops then use the Clubs skill).</summary>
    internal static Skills.SkillType Type { get; private set; } = Skills.SkillType.None;

    private static bool _iconSet;

    /// <summary>Gives the skill its icon (the first crop icon that rendered). Expected outcome: true when the icon was set now (false when it was set before, the skill is not registered or there is no icon).</summary>
    internal static bool SetIcon(Sprite icon)
    {
        bool set = !_iconSet && icon != null && Type != Skills.SkillType.None && KySkills.SetIcon(Type, icon);
        if (set) _iconSet = true;
        ModLog.Verbose("CropSkill.SetIcon: icon=" + (icon != null) + ", already set=" + _iconSet + ", skill type " + (int)Type + " -> " + set);
        return set;
    }

    /// <summary>Registers the skill. Must run before the game creates its Skills object (Plugin.Awake).</summary>
    internal static void Register()
    {
        try
        {
            Type = KySkills.Add(SkillIdentifier, SkillName, "Skill with the riding crop. The higher it is, the harder the crop hits.", null, 1f);
            ModLog.Verbose("CropSkill: skill '" + SkillName + "' registered (id " + (int)Type + ")");
        }
        catch (Exception ex)
        {
            ModLog.Error("CropSkill: could not register the '" + SkillName + "' skill; the crops will use the Clubs skill. " + ex.Message);
            Type = Skills.SkillType.None;
        }
    }
}
