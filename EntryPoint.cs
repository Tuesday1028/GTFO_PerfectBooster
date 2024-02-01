using BepInEx;
using BepInEx.Unity.IL2CPP;
using TheArchive;
using TheArchive.Core;
using TheArchive.Core.Localization;

namespace Hikaria.PerfectBooster;

[BepInDependency(ArchiveMod.GUID, BepInDependency.DependencyFlags.HardDependency)]
[BepInPlugin(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
public class EntryPoint : BasePlugin, IArchiveModule
{
    public override void Load()
    {
        Instance = this;

        ArchiveMod.RegisterArchiveModule(typeof(EntryPoint));

        Logs.LogMessage("OK");
    }

    public void Init()
    {
    }

    public void OnExit()
    {
    }

    public void OnLateUpdate()
    {
    }

    public void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
    }
    public bool ApplyHarmonyPatches => false;

    public bool UsesLegacyPatches => false;

    public ArchiveLegacyPatcher Patcher { get; set; }

    public static EntryPoint Instance { get; private set; }

    public string ModuleGroup => "Perfect Booster";

    public Dictionary<Language, string> ModuleGroupLanguages => new()
    {
        { Language.Chinese, "完美强化剂" },
        { Language.English, "Perfect Booster" }
    };
}
