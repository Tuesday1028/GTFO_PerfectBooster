using BoosterImplants;
using Clonesoft.Json;
using GameData;
using Hikaria.PerfectBooster.Managers;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using static Hikaria.PerfectBooster.Managers.BoosterImplantTemplateManager;

namespace Hikaria.PerfectBooster.Features;

[DisallowInGameToggle]
[EnableFeatureByDefault]
public class PerfectBooster : Feature
{
    public override string Name => "Perfect Booster";

    public override string Group => FeatureGroups.QualityOfLife;

    public override bool RequiresRestart => true;

    [FeatureConfig]
    public static PerfectBoosterSetting Settings { get; set; }

    public class PerfectBoosterSetting
    {
        [FSDisplayName("快速刷取")]
        public bool EnableBoosterFarmer { get; set; } = true;
        [FSDisplayName("完美强化剂")]
        public bool EnablePerfectBooster { get; set; } = true;
        [FSDisplayName("禁用消耗")]
        public bool DisableBoosterConsume { get; set; } = true;
        [FSHeader("模板首选项")]
        [FSDisplayName("启用模板首选项")]
        public bool EnableBoosterTemplatePreference { get => BoosterImplantTemplateManager.EnableBoosterTemplatePreference; set => BoosterImplantTemplateManager.EnableBoosterTemplatePreference = value; }
        [FSDisplayName("模板首选项设置")]
        public List<BoosterImplantTemplatePreference> BoosterTemplatePreferences { get => BoosterImplantTemplateManager.BoosterTemplatePreferences.ToList(); set { } }

        [FSHide]
        [FSDisplayName("正面效果倍率")]
        public float BoosterPositiveEffectMultiplier { get => BoosterImplantTemplateManager.BoosterPositiveEffectMultiplier; set => BoosterImplantTemplateManager.BoosterPositiveEffectMultiplier = value; }
        [FSHide]
        [FSDisplayName("禁用条件")]
        public bool DisableBoosterConditions { get => BoosterImplantTemplateManager.DisableBoosterConditions; set => BoosterImplantTemplateManager.DisableBoosterConditions = value; }
        [FSHide]
        [FSDisplayName("禁用负面效果")]
        public bool DisableBoosterNegativeEffects { get => BoosterImplantTemplateManager.DisableBoosterNegativeEffects; set => BoosterImplantTemplateManager.DisableBoosterNegativeEffects = value; }

    }

    public class BoosterImplantTemplatePreference
    {
        public BoosterImplantTemplatePreference(BoosterImplantTemplate template)
        {
            for (int i = 0; i < template.EffectGroups.Count; i++)
            {
                EffectsTemplates.Add(new(i, template.EffectGroups[i]));
            }
            for (int i = 0; i < template.ConditionGroups.Count; i++)
            {
                ConditionsTemplates.Add(new(i, template.ConditionGroups[i]));
            }
            TemplateName = template.TemplateDataBlock.PublicName.ToString();
            TemplateId = template.BoosterImplantID;
            TemplateCategory = template.ImplantCategory;
        }

        public BoosterImplantTemplatePreference()
        {
        }

        [FSSeparator]
        [FSReadOnly]
        [FSDisplayName("强化剂名称")]
        public string TemplateName { get; set; } = string.Empty;

        [FSReadOnly]
        [FSDisplayName("强化剂ID")]
        public uint TemplateId { get; set; } = 0;

        [FSReadOnly]
        [FSDisplayName("强化剂类别")]
        public BoosterImplantCategory TemplateCategory { get; set; } = BoosterImplantCategory._COUNT;

        [FSDisplayName("首选效果组索引")]
        public int EffectsGroupIndex { get; set; } = -1;

        [FSDisplayName("首选条件组索引")]
        public int ConditionsGroupIndex { get; set; } = -1;

        [JsonIgnore]
        [FSReadOnly]
        [FSDisplayName("可选效果模板")]
        public List<BoosterImplantEffectGroupPreferenceEntry> EffectsTemplates { get; set; } = new();

        [JsonIgnore]
        [FSReadOnly]
        [FSDisplayName("可选条件模板")]
        public List<BoosterImplantConditionGroupPreferenceEntry> ConditionsTemplates { get; set; } = new();
    }

    public class BoosterImplantEffectGroupPreferenceEntry
    {
        public BoosterImplantEffectGroupPreferenceEntry(int index, List<BoosterImplantEffectTemplate> effectGroup)
        {
            Index = index;
            Effects.Clear();
            for (int i = 0; i < effectGroup.Count; i++)
            {
                var block = BoosterImplantEffectDataBlock.GetBlock(effectGroup[i].BoosterImplantEffect);
                if (block != null)
                {
                    Effects.Add(new(block));
                }
            }
        }

        [FSSeparator]
        [FSReadOnly]
        [FSDisplayName("效果组索引")]
        public int Index { get; set; }

        [FSReadOnly]
        [FSInline]
        [FSDisplayName("效果列表")]
        public List<EffectEntry> Effects { get; set; } = new();

        public class EffectEntry
        {
            public EffectEntry(BoosterImplantEffectDataBlock block)
            {
                Effect = block.Effect;
            }

            [FSInline]
            [FSReadOnly]
            [FSDisplayName("效果")]
            public AgentModifier Effect { get; set; }
        }
    }

    public class BoosterImplantConditionGroupPreferenceEntry
    {
        public BoosterImplantConditionGroupPreferenceEntry(int index, List<uint> conditionGroup)
        {
            Index = index;
            Conditions.Clear();
            for (int i = 0; i < conditionGroup.Count; i++)
            {
                var block = BoosterImplantConditionDataBlock.GetBlock(conditionGroup[i]);
                if (block != null)
                {
                    Conditions.Add(new(block));
                }
            }
        }

        [FSSeparator]
        [FSReadOnly]
        [FSDisplayName("条件组索引")]
        public int Index { get; set; }

        [FSReadOnly]
        [FSInline]
        [FSDisplayName("条件列表")]
        public List<ConditionEntry> Conditions { get; set; } = new();
        public class ConditionEntry
        {
            public ConditionEntry(BoosterImplantConditionDataBlock block)
            {
                Condition = block.Condition;
            }

            [FSInline]
            [FSReadOnly]
            [FSDisplayName("条件")]
            public BoosterCondition Condition { get; set; }
        }
    }

    [ArchivePatch(typeof(LocalizationManager), nameof(LocalizationManager.Setup))]
    private class LocalizationManager__Setup__Patch
    {
        private static void Postfix()
        {
            LoadTemplatePreferences();
        }
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
                    if (TryGetBoosterImplantTemplate(boosterImplant, out BoosterImplantTemplate template, out var effectGroup, out var conditions))
                    {
                        if (TryGetBoosterImplantTemplatePreference(boosterImplant, template, out var preferedEffectGroup, out var preferedConditionGroup))
                            ApplyPerfectBoosterFromTemplate(boosterImplant, preferedEffectGroup, preferedConditionGroup);
                        else
                            ApplyPerfectBoosterFromTemplate(boosterImplant, effectGroup, conditions);
                    }
                }
            }
        }
    }

    [ArchivePatch(typeof(DropServerManager), nameof(DropServerManager.NewGameSession))]
    internal static class DropServerManager__NewGameSession__Patch
    {
        public static void Prefix(ref uint[] boosterIds)
        {
            if (Settings.DisableBoosterConsume)
            {
                boosterIds = null;
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

    public override void OnQuit()
    {
        SaveTemplatePreferences();
    }

    public override void OnGameDataInitialized()
    {
        LoadTemplateData();
    }
}
