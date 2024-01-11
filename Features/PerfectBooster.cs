using BoosterImplants;
using CellMenu;
using Clonesoft.Json;
using GameData;
using Hikaria.PerfectBooster.Managers;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.FeaturesAPI.Components;
using static Hikaria.PerfectBooster.Managers.BoosterImplantTemplateManager;
using static Hikaria.PerfectBooster.Managers.CustomBoosterImplantManager;
using static Hikaria.PerfectBooster.Managers.CustomBoosterImplantManager.CustomBoosterImplant;

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

        [JsonIgnore]
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

        [FSHide]
        [FSDisplayName("强化剂自定义")]
        [FSDescription("自定义将导致完美强化剂与模板首选项以及其他作弊选项失效")]
        public bool EnableCustomBooster { get => BoosterImplantTemplateManager.EnableCustomBooster; set => BoosterImplantTemplateManager.EnableCustomBooster = value; }

        [FSHide]
        [FSDisplayName("通过现有强化剂生成自定义强化剂")]
        public FButton CreateCustomBoosterFromInventory { get; set; } = new("生成", "生成自定义强化剂", CreateCustomBoosterImplantsFromInventory);

        [FSHide]
        [FSDisplayName("通过设置文件重载自定义强化剂")]
        public FButton LoadCustomBoosterFromSettings { get; set; } = new("重载", "重载自定义强化剂设置", LoadCustomBoosterImplantsFromSettings);

        [FSHide]
        [FSDisplayName("应用自定义强化剂")]
        public FButton ApplyCustomBoosters { get; set; } = new("应用", "应用自定义强化剂", ApplyCustomBoosterImplants);

        [JsonIgnore]
        [FSHide]
        [FSReadOnly]
        [FSDisplayName("编辑自定义强化剂")]
        public Dictionary<BoosterImplantCategory, CustomBoosterImplantEntryListEntry> CustomBoosterImplantsEntry
        {
            get
            {
                Dictionary<BoosterImplantCategory, CustomBoosterImplantEntryListEntry> result = new();
                for (int i = 0; i < 3; i++)
                {
                    var category = (BoosterImplantCategory)i;
                    var entries = new List<CustomBoosterImplantEntry>();
                    for (int j = 0; j < CustomBoosterImplants[category].Count; j++)
                    {
                        entries.Add(new(CustomBoosterImplants[category][j]));
                    }
                    result[category] = new(entries);
                }
                return result;
            }
            set
            {
            }
        }
    }

    public class CustomBoosterImplantEntryListEntry
    {
        public CustomBoosterImplantEntryListEntry(List<CustomBoosterImplantEntry> entries)
        {
            Entries = entries;
        }

        [FSInline]
        [FSDisplayName("强化剂列表")]
        public List<CustomBoosterImplantEntry> Entries { get; set; } = new();
    }

    public class CustomBoosterImplantEntry
    {
        public CustomBoosterImplantEntry(CustomBoosterImplant implant)
        {
            Implant = implant;
            Modifier = new(implant);
        }

        [FSSeparator]
        [FSReadOnly]
        [FSDisplayName("名称")]
        public string Name { get => Implant.Name; set { } }
        [FSReadOnly]
        [FSDisplayName("类别")]
        public BoosterImplantCategory Category { get => Implant.Category; set { } }
        [FSReadOnly]
        [FSDisplayName("ID")]
        public uint TemplateId { get => Implant.TemplateId; set { } }

        [FSDisplayName("效果列表")]
        public List<CustomBoosterImplantEffectEntry> Effect
        {
            get
            {
                List<CustomBoosterImplantEffectEntry> result = new();
                for (int i = 0; i < Implant.Effects.Count; i++)
                {
                    result.Add(new(Implant.Effects[i]));
                }
                return result;
            }
            set
            {
            }
        }

        [FSDisplayName("条件列表")]
        public List<CustomBoosterImplantConditionEntry> Conditions
        {
            get
            {
                List<CustomBoosterImplantConditionEntry> conditions = new();
                for (int i = 0; i < Implant.Conditions.Count; i++)
                {
                    conditions.Add(new(i, Implant));
                }
                return conditions;
            }
            set
            {
            }
        }

        [FSDisplayName("修改")]
        public BoosterImplantModifier Modifier { get; set; }

        [FSIgnore]
        private CustomBoosterImplant Implant { get; set; }
    }

    public enum ModifyType
    {
        Add,
        Remove,
        Modify
    }

    public enum ModifyTarget
    {
        Effect,
        Condition
    }

    public class BoosterImplantModifier
    {
        public BoosterImplantModifier(CustomBoosterImplant implant)
        {
            Implant = implant;
            Modify = new FButton("应用", "应用修改", DoModify);
        }

        [FSDisplayName("ID")]
        public uint Id { get; set; } = 0;

        [FSDisplayName("值")]
        public float Value { get; set; } = 1f;

        [FSDisplayName("修改模式")]
        public ModifyType type { get; set; } = ModifyType.Modify;

        [FSDisplayName("修改对象")]
        public ModifyTarget target { get; set; } = ModifyTarget.Effect;

        [FSDisplayName("应用修改")]
        public FButton Modify { get; set; }

        private void DoModify()
        {
            var effects = Implant.Effects;
            var conditions = Implant.Conditions;
            switch (type)
            {
                case ModifyType.Modify:
                    if (target == ModifyTarget.Effect)
                    {
                        var index = effects.FindIndex(p => p.Id == Id);
                        if (index == -1) return;
                        effects[index].Value = Value;
                    }
                    else
                    {
                        var index = conditions.FindIndex(p => p == Id);
                        if (index == -1) return;
                        conditions[index] = Id;
                    }
                    break;
                case ModifyType.Add:
                    if (target == ModifyTarget.Effect)
                    {
                        var index = effects.FindIndex(p => p.Id == Id);
                        if (index != -1) return;
                        effects.Add(new() { Id = Id, Value = Value });
                    }
                    else
                    {
                        var index = conditions.FindIndex(p => p == Id);
                        if (index != -1) return;
                        conditions.Add(Id);
                    }
                    break;
                case ModifyType.Remove:
                    if (target == ModifyTarget.Effect)
                    {
                        var index = effects.FindIndex(p => p.Id == Id);
                        if (index == -1) return;
                        effects.RemoveAt(index);
                    }
                    else
                    {
                        var index = conditions.FindIndex(p => p == Id);
                        if (index == -1) return;
                        conditions.RemoveAt(index);
                    }
                    break;
            }
        }

        private CustomBoosterImplant Implant { get; set; }
    }

    public class CustomBoosterImplantEffectEntry
    {
        public CustomBoosterImplantEffectEntry(Effect effect)
        {
            Effect = effect;
        }

        [FSSeparator]
        [FSDisplayName("效果ID")]
        public uint Id { get => Effect.Id; set => Effect.Id = value; }
        [FSDisplayName("效果数值")]
        public float Value { get => Effect.Value; set => Effect.Value = value; }

        [FSIgnore]
        private Effect Effect { get; set; }
    }

    public class CustomBoosterImplantConditionEntry
    {
        public CustomBoosterImplantConditionEntry(int index, CustomBoosterImplant implant)
        {
            Index = index;
            Implant = implant;
        }

        [FSIgnore]
        public int Index { get; set; }

        [FSSeparator]
        [FSDisplayName("条件")]
        public uint Condition { get => Implant.Conditions[Index]; set => Implant.Conditions[Index] = value; }

        [FSIgnore]
        private CustomBoosterImplant Implant { get; set; }
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
        [FSDisplayName("名称")]
        public string TemplateName { get; set; } = string.Empty;

        [FSReadOnly]
        [FSDisplayName("ID")]
        public uint TemplateId { get; set; } = 0;

        [FSReadOnly]
        [FSDisplayName("类别")]
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

    [ArchivePatch(typeof(PersistentInventoryManager), nameof(PersistentInventoryManager.Setup))]
    private class PersistentInventoryManager__Setup__Patch
    {
        private static void Postfix(PersistentInventoryManager __instance)
        {
            __instance.OnBoosterImplantInventoryChanged += new Action(delegate ()
            {
                if (Settings.EnableCustomBooster)
                {
                    ApplyCustomBoosterImplants();
                }
            });
        }
    }

    [ArchivePatch(typeof(BoosterImplantManager), nameof(BoosterImplantManager.OnActiveBoosterImplantsChanged))]
    private class BoosterImplantManager__OnActiveBoosterImplantsChanged__Patch
    {
        private static void Prefix()
        {
            if (!Settings.EnableCustomBooster && Settings.EnablePerfectBooster)
            {
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
    }

    [ArchivePatch(typeof(CM_PageLoadout), nameof(CM_PageLoadout.ProcessBoosterImplantEvents))]
    private class CM_PageLoadout__ProcessBoosterImplantEvents__Patch
    {
        private static bool Prefix()
        {
            return false;
        }
    }

    [ArchivePatch(typeof(DropServerManager), nameof(DropServerManager.NewGameSession))]
    internal static class DropServerManager__NewGameSession__Patch
    {
        public static void Prefix(ref uint[] boosterIds)
        {
            if (Settings.DisableBoosterConsume || Settings.EnableCustomBooster)
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
            return !(Settings.DisableBoosterConsume || Settings.EnableCustomBooster);
        }
    }

    [ArchivePatch(typeof(ArtifactInventory), nameof(ArtifactInventory.GetArtifactCount))]
    private class ArtifactInventory__GetArtifactCount__Patch
    {
        private static bool Prefix(ref int __result)
        {
            if (Settings.EnableBoosterFarmer)
            {
                __result = 1000;
                return false;
            }
            if (Settings.EnableCustomBooster)
            {
                __result = 0;
                return false;
            }
            return true;
        }
    }

    public override void OnQuit()
    {
        SaveTemplatePreferences();
        SaveCustomBoosterImplants();
    }

    public override void OnGameDataInitialized()
    {
        LoadTemplateData();
        LoadCustomBoosterImplantsFromSettings();
    }
}
