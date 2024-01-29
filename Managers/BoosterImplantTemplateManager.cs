using BoosterImplants;
using Clonesoft.Json;
using GameData;
using Hikaria.PerfectBooster.Features;
using TheArchive.Core.ModulesAPI;

namespace Hikaria.PerfectBooster.Managers;

public static class BoosterImplantTemplateManager
{
    public static float BoosterPositiveEffectMultiplier { get; set; } = 1f;
    public static bool DisableBoosterConditions { get; set; } = false;
    public static bool DisableBoosterNegativeEffects { get; set; } = false;
    public static bool EnableCustomPerfectBooster { get; set; } = false;
    public static bool EnableCustomBooster { get; set; } = false;

    public static CustomSetting<Dictionary<BoosterImplantCategory, List<bBoosterImplantTemplate>>> BoosterImplantTemplatesLookup { get; set; } = new("BoosterImplantTemplatesLookup", new()
    {
        { BoosterImplantCategory.Muted, new() },
        { BoosterImplantCategory.Bold, new() },
        { BoosterImplantCategory.Aggressive, new() }
    }, null, LoadingTime.None, false);

    public class bBoosterImplantTemplate
    {
        public bBoosterImplantTemplate(BoosterImplantTemplate template)
        {
            BoosterImplantID = template.BoosterImplantID;
            ImplantCategory = template.ImplantCategory;
            int index = 0;
            foreach (var group in template.EffectGroups)
            {
                var list = new List<bBoosterImplantEffectTemplate>();
                foreach (var effectTemplate in group)
                {
                    list.Add(new(effectTemplate));
                }
                EffectGroups.Add(index++, list);
            }
            index = 0;
            foreach (var group in template.ConditionGroups)
            {
                var list = new List<BoosterCondition>();
                foreach (var condition in group)
                {
                    list.Add(BoosterImplantConditionDataBlock.GetBlock(condition).Condition);
                }
                ConditionGroups.Add(index++, list);
            }
        }

        public uint BoosterImplantID { get; set; }
        public BoosterImplantCategory ImplantCategory { get; set; }
        public Dictionary<int, List<bBoosterImplantEffectTemplate>> EffectGroups { get; set; } = new();
        public Dictionary<int, List<BoosterCondition>> ConditionGroups { get; set; } = new();
    }

    public class bBoosterImplantEffectTemplate
    {
        public bBoosterImplantEffectTemplate(BoosterImplantEffectTemplate effectTemplate)
        {
            BoosterImplantEffect = effectTemplate.BoosterImplantEffect;
            AgentModifier = BoosterImplantEffectDataBlock.GetBlock(BoosterImplantEffect).Effect;
            EffectMaxValue = effectTemplate.EffectMaxValue;
            EffectMinValue = effectTemplate.EffectMinValue;
        }

        public uint BoosterImplantEffect { get; set; }
        public AgentModifier AgentModifier { get; set; }
        public float EffectMaxValue { get; set; }
        public float EffectMinValue { get; set; }
    }

    public static void LoadTemplateData()
    {
        BoosterImplantTemplates.Clear();
        var templates = BoosterImplantTemplateDataBlock.GetAllBlocksForEditor();
        for (int i = 0; i < templates.Count; i++)
        {
            BoosterImplantTemplates.Add(new(templates[i]));
        }

        BoosterImplantTemplatesLookup.Value = new()
        {
            { BoosterImplantCategory.Muted, new() },
            { BoosterImplantCategory.Bold, new() },
            { BoosterImplantCategory.Aggressive, new() }
        };
        for (int i = 0; i < BoosterImplantTemplates.Count; i++)
        {
            BoosterImplantTemplatesLookup.Value[BoosterImplantTemplates[i].ImplantCategory].Add(new (BoosterImplantTemplates[i]));
        }
        BoosterImplantTemplatesLookup.Save();
    }

    public static void ApplyPerfectBoosterFromTemplate(BoosterImplant boosterImplant, List<BoosterImplantEffectTemplate> effectGroup, List<uint> conditions)
    {
        List<BoosterImplant.Effect> effects = new();
        foreach (var effect in effectGroup)
        {
            effects.Add(new()
            {
                Id = effect.BoosterImplantEffect,
                Value = effect.EffectMaxValue <= 1 && DisableBoosterNegativeEffects ? 1f : (effect.EffectMaxValue - 1f) * BoosterPositiveEffectMultiplier + 1f
            });
        }
        boosterImplant.Effects = effects.ToArray();
        boosterImplant.Conditions = DisableBoosterConditions ? Array.Empty<uint>() : conditions.ToArray();
    }

    public static bool TryGetBoosterImplantTemplate(BoosterImplant boosterImplant, out BoosterImplantTemplate template, out List<BoosterImplantEffectTemplate> effectGroup, out List<uint> conditionGroup, out int effectGroupIndex, out int conditionGroupIndex)
    {
        effectGroup = new();
        conditionGroup = new();
        effectGroupIndex = -1;
        conditionGroupIndex = -1;
        uint persistenID = boosterImplant.TemplateId;
        template = BoosterImplantTemplates.FirstOrDefault(p => p.BoosterImplantID == persistenID && p.ImplantCategory == boosterImplant.Category);

        if (template == null || template.TemplateDataBlock == null)
        {
            return false;
        }

        var conditionGroups = template.ConditionGroups;
        int conditionCount = boosterImplant.Conditions.Count;
        bool ConditionMatch = conditionCount == 0;
        var conditions = boosterImplant.Conditions;
        if (conditionCount > 0)
        {
            for (int i = 0; i < conditionGroups.Count; i++)
            {
                if (conditionCount != conditionGroups[i].Count)
                {
                    continue;
                }

                bool flag1 = conditions.All(p => conditionGroups[i].Any(q => q == p));
                bool flag2 = conditionGroups[i].All(p => conditions.Any(q => q == p));
                if (flag1 && flag2)
                {
                    ConditionMatch = true;
                    conditionGroup = conditionGroups[i];
                    conditionGroupIndex = i;
                    break;
                }
            }
        }
        if (!ConditionMatch)
        {
            return false;
        }

        int effectCount = boosterImplant.Effects.Count;
        bool EffectMatch = false;
        var effectGroups = template.EffectGroups;
        var effects = boosterImplant.Effects.ToList();
        for (int i = 0; i < effectGroups.Count; i++)
        {
            if (effectGroups[i].Count != effectCount)
            {
                continue;
            }
            for (int j = 0; j < effectGroups[i].Count; j++)
            {
                bool flag1 = effects.All(p => effectGroups[i].Any(q => q.BoosterImplantEffect == p.Id));
                bool flag2 = effectGroups[i].All(p => effects.Any(q => q.Id == p.BoosterImplantEffect));
                if (flag1 && flag2)
                {
                    EffectMatch = true;
                    effectGroup = effectGroups[i];
                    effectGroupIndex = i;
                    break;
                }
            }
        }
        if (!EffectMatch)
        {
            return false;
        }

        return true;
    }

    public static List<BoosterImplantTemplate> BoosterImplantTemplates { get; } = new();

    public class BoosterImplantEffectTemplate
    {
        public BoosterImplantEffectTemplate(BoosterImplantEffectInstance effect)
        {
            EffectMaxValue = effect.MaxValue;
            EffectMinValue = effect.MinValue;
            BoosterImplantEffect = effect.BoosterImplantEffect;
        }

        public uint BoosterImplantEffect { get; set; }
        public float EffectMaxValue { get; set; }
        public float EffectMinValue { get; set; }
    }

    public class BoosterImplantTemplate
    {
        public BoosterImplantTemplate(BoosterImplantTemplateDataBlock block)
        {
            TemplateDataBlock = block;

            BoosterImplantID = block.persistentID;
            ImplantCategory = block.ImplantCategory;

            for (int i = 0; i < block.Effects.Count; i++)
            {
                Effects.Add(new(block.Effects[i]));
            }
            for (int i = 0; i < block.RandomEffects.Count; i++)
            {
                List<BoosterImplantEffectTemplate> randomEffects = new();
                for (int j = 0; j < block.RandomEffects[i].Count; j++)
                {
                    randomEffects.Add(new(block.RandomEffects[i][j]));
                }
                RandomEffects.Add(randomEffects);
            }

            Conditions.AddRange(block.Conditions.ToArray());
            RandomConditions.AddRange(block.RandomConditions.ToArray());

            EffectGroups = GenerateEffectGroups();
            ConditionGroups = GenerateConditionGroups();
        }

        private List<List<BoosterImplantEffectTemplate>> GenerateEffectGroups()
        {
            List<List<BoosterImplantEffectTemplate>> effectGroups = new();

            List<List<BoosterImplantEffectTemplate>> combinations = GetNElementCombinations(RandomEffects);
            for (int i = 0; i < combinations.Count; i++)
            {
                List<BoosterImplantEffectTemplate> effectGroup = Effects.ToList();
                effectGroup.AddRange(combinations[i]);
                effectGroups.Add(effectGroup);
            }
            return effectGroups;
        }

        private List<List<uint>> GenerateConditionGroups()
        {
            List<List<uint>> conditions = new() { Conditions, RandomConditions };
            List<List<uint>> combinations = GetNElementCombinations(conditions);
            return combinations;
        }

        private static List<List<T>> GetNElementCombinations<T>(List<List<T>> lists)
        {
            List<List<T>> combinations = new List<List<T>>();

            GetNElementCombinationsHelper(lists, new List<T>(), 0, combinations);

            return combinations;
        }

        private static void GetNElementCombinationsHelper<T>(List<List<T>> lists, List<T> currentCombination, int currentIndex, List<List<T>> combinations)
        {
            if (currentIndex == lists.Count)
            {
                combinations.Add(new List<T>(currentCombination));
                return;
            }

            List<T> currentList = lists[currentIndex];

            if (currentList.Count == 0)
            {
                GetNElementCombinationsHelper(lists, currentCombination, currentIndex + 1, combinations);
                return;
            }

            foreach (T item in currentList)
            {
                currentCombination.Add(item);
                GetNElementCombinationsHelper(lists, currentCombination, currentIndex + 1, combinations);
                currentCombination.RemoveAt(currentCombination.Count - 1);
            }
        }

        public uint BoosterImplantID { get; set; }
        public BoosterImplantCategory ImplantCategory { get; set; }
        [JsonIgnore]
        public List<BoosterImplantEffectTemplate> Effects { get; set; } = new();
        [JsonIgnore]
        public List<List<BoosterImplantEffectTemplate>> RandomEffects { get; set; } = new();
        [JsonIgnore]
        public List<uint> Conditions { get; set; } = new();
        [JsonIgnore]
        public List<uint> RandomConditions { get; set; } = new();

        [JsonIgnore]
        public BoosterImplantTemplateDataBlock TemplateDataBlock { get; private set; } = null;

        public List<List<BoosterImplantEffectTemplate>> EffectGroups { get; set; } = new();
        public List<List<uint>> ConditionGroups { get; set; } = new();
    }
}
