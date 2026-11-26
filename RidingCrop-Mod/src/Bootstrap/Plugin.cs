using System.IO;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using KydlCore.Common;
using RidingCrop.Configuration;
using RidingCrop.Bootstrap;

namespace RidingCrop;

/// <summary>
/// Adds seven craftable one-handed weapons, the "Riding Crop" family (a weakest-to-strongest tier ladder), their own skill and sounds.
/// Order in Awake matters: log first, then the config, then the features (the skill must exist before the game creates its Skills object).
/// </summary>
[BepInPlugin(ModGuid, ModName, ModVersion)]
[BepInDependency(KydlCore.Bootstrap.Plugin.ModGuid, "1.2.0")]     // the KY hub (manifest: KydlDev-KydlCore-1.1.3, same version)
// Network check: ModSync.cs (KydlCore 1.2.0 KyNetwork, EveryoneMustHave; no Jotunn - MIGRATION_PLAN part 0).
public class Plugin : BaseUnityPlugin
{
    // ModGuid never changes (it is the config file name: BepInEx/config/com.kydldev.RidingCrop.cfg).
    // ModVersion = manifest.json version_number = newest CHANGELOG heading (tests check it).
    public const string ModGuid = "com.kydldev.RidingCrop";
    public const string ModName = "RidingCrop";
    public const string ModVersion = "2.0.2";

    private Harmony _harmony;
    private ConfigReloader _cfgWatcher;
    private ModInstaller _installer;

    /// <summary>True until Awake is done. Items and the skill can only be added while it is true (the game cannot take them later).</summary>
    internal static bool InStartup { get; private set; } = true;

    private void Awake()
    {
        ModLog.Init(Logger, ModName, () => ModSettings.DebugOn);
        ModLog.Info(ModName + " " + ModVersion + " loaded (dll built " + File.GetLastWriteTime(Assembly.GetExecutingAssembly().Location).ToString("yyyy-MM-dd HH:mm:ss") + ")");

        ModSettings.Bind(Config);

        int adminShared = ModSync.ShareAdminOnly(Config);     // network check + server-decided settings (KydlCore KyNetwork)
        RidingCrop.Core.RuleLog.Sink = ModLog.Verbose;   // the pure code logs through ModLog (PROJECT_RULES 8, KydlCore D-103)
        RidingCrop.Core.RuleLog.DebugOn = () => ModSettings.DebugLog != null && ModSettings.DebugLog.Value;
        ConfigMigration.Run(Config);
        _cfgWatcher = new ConfigReloader(Config);

        _installer = new ModInstaller(Config);
        bool installed = _installer.Install();
        Config.SettingChanged += (s, e) => { if (_installer != null) _installer.Refresh(); };
        ModLog.Verbose("Plugin: ModInstaller " + (installed ? "installed" : "reported a problem"));

        _harmony = new Harmony(ModGuid);
        bool patched = PatchInstaller.Apply(_harmony, Assembly.GetExecutingAssembly());
        ModLog.Info(ModName + " ready" + (patched ? "" : " (patching failed, see the log)"));
        InStartup = false;
    }

    private void Update()
    {
        if (_cfgWatcher != null) _cfgWatcher.Tick();
    }

    private void OnDestroy()
    {
        if (_cfgWatcher != null) { _cfgWatcher.Dispose(); _cfgWatcher = null; }
        if (_installer != null) { _installer.Uninstall(); _installer = null; }
        if (_harmony != null) _harmony.UnpatchSelf();
        ModLog.Verbose("Plugin: destroyed (patches removed)");
    }
}
