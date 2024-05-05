using TheArchive.Core;
using TheArchive.Core.Attributes;
using TheArchive.Core.Localization;

namespace Hikaria.BoosterTweaker;

[ArchiveModule(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
public class EntryPoint : IArchiveModule
{
    public void Init()
    {
        Instance = this;
        Logs.LogMessage("OK");
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
