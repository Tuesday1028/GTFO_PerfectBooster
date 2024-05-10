using Hikaria.BoosterTweaker.Detours;
using Hikaria.Core.Utilities;
using TheArchive.Core;
using TheArchive.Core.Attributes;
using TheArchive.Core.Localization;

namespace Hikaria.BoosterTweaker;

[ArchiveDependency(Core.PluginInfo.GUID)]
[ArchiveModule(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
public class EntryPoint : IArchiveModule
{
    public void Init()
    {
        Instance = this;

        EasyDetour.CreateAndApply<PersistentInventoryManager__UpdateBoosterImplants__NativeDetour>(out _);

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

    public string ModuleGroup => "Booster Tweaker";

    public Dictionary<Language, string> ModuleGroupLanguages => new()
    {
        { Language.Chinese, "强化剂调节" },
        { Language.English, "Booster Tweaker" }
    };
}

