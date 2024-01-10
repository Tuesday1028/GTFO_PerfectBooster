using BoosterImplants;
using Hikaria.PerfectBooster.Managers;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;

namespace Hikaria.PerfectBooster.Features;

[DisallowInGameToggle]
[EnableFeatureByDefault]
internal class PerfectBooster : Feature
{
    public override string Name => "Perfect Booster";

    [FeatureConfig]
    public static PerfectBoosterSetting Settings { get; set; }

    public class PerfectBoosterSetting
    {
        [FSDisplayName("强化剂刷取")]
        public bool EnableBoosterFarmer { get; set; } = true;
        [FSDisplayName("完美强化剂")]
        public bool EnablePerfectBooster { get; set; } = true;
        [FSDisplayName("强化剂不消耗")]
        public bool DisableBoosterConsume { get; set; } = true;
    }

    [ArchivePatch(typeof(BoosterImplantManager), nameof(BoosterImplantManager.OnActiveBoosterImplantsChanged))]
    private class BoosterImplantManager__OnActiveBoosterImplantsChanged__Patch
    {
        private static void Prefix()
        {
            if (!Settings.EnablePerfectBooster)
                return;

            for (int i = 0; i < PersistentInventoryManager.Current.m_boosterImplantInventory.Categories.Count; i++)
            {
                var category = PersistentInventoryManager.Current.m_boosterImplantInventory.Categories[i];
                var inventory = category.Inventory;
                for (int j = 0; j < inventory.Count; j++)
                {
                    var boosterImplant = inventory[j].Implant;
                    if (BoosterImplantTemplateManager.TryGetBoosterImplantTemplate(boosterImplant, out var template, out var effectGroup, out var conditions))
                    {
                        BoosterImplantTemplateManager.ApplyPerfectBoosterFromTemplate(boosterImplant, effectGroup, conditions);
                    }
                }
            }
        }
    }


    [ArchivePatch(typeof(DropServerGameSession), nameof(DropServerGameSession.ConsumeBoosters))]
    private class DropServerGameSession__ConsumeBoosters__Patch
    {
        private static bool Prefix()
        {
            return !Settings.DisableBoosterConsume;
        }
    }

    [ArchivePatch(typeof(ArtifactInventory), nameof(ArtifactInventory.GetArtifactCount))]
    private class ArtifactInventory__GetArtifactCount__Patch
    {
        private static void Postfix(ref int __result)
        {
            if (Settings.EnableBoosterFarmer)
            {
                __result = 10000;
            }
        }
    }

    public override void OnGameDataInitialized()
    {
        BoosterImplantTemplateManager.LoadData();
    }
}
