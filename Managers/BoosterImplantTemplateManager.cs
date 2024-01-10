using GameData;
using UnityEngine;

namespace Hikaria.PerfectBooster.Managers;

public static class BoosterImplantTemplateManager
{
    public static void LoadData()
    {
        BoosterImplantTemplates.Clear();
        var templates = BoosterImplantTemplateDataBlock.GetAllBlocksForEditor();
        for (int i = 0; i < templates.Count; i++)
        {
            BoosterImplantTemplates[templates[i].persistentID] = new(templates[i]);
        }
    }


    public static void ApplyPerfectBoosterFromTemplate(BoosterImplant boosterImplant, List<BoosterImplantEffectTemplate> effectGroup, List<uint> conditions)
    {
        var effectCount = boosterImplant.Effects.Count;
        var effects = boosterImplant.Effects.ToArray();
        for (int i = 0; i < effectCount; i++)
        {
            var effect = effectGroup.FirstOrDefault(p => p.BoosterImplantEffect == boosterImplant.Effects[i].Id);
            float targetValue = effect.EffectMaxValue > 1 ? effect.EffectMaxValue - 0.0001f : effect.EffectMaxValue + 0.0001f;
            effects[i].Value = targetValue;
        }
        boosterImplant.Effects = effects;
        boosterImplant.Conditions = conditions.ToArray();
    }

    public static bool TryGetBoosterImplantTemplate(BoosterImplant boosterImplant, out BoosterImplantTemplate template, out List<BoosterImplantEffectTemplate> effectGroup, out List<uint> conditionGroup)
    {
        effectGroup = null;
        conditionGroup = null;
        uint persistenID = boosterImplant.TemplateId;
        Logs.LogMessage($"{boosterImplant.GetCompositPublicName()}, {boosterImplant.TemplateId}");
        if (!BoosterImplantTemplates.TryGetValue(persistenID, out template))
        {
            return false;
        }

        if (boosterImplant.Category != template.ImplantCategory)
        {
            return false;
        }

        var conditionGroups = template.GenerateConditionGroups();
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
        var effectGroups = template.GenerateEffectGroups();
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

    public static Dictionary<uint, BoosterImplantTemplate> BoosterImplantTemplates = new();

    public static Dictionary<uint, List<uint>> PreferedConditions = new();

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
            DurationRange = block.DurationRange;
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
        }

        public List<List<BoosterImplantEffectTemplate>> GenerateEffectGroups()
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

        public List<List<uint>> GenerateConditionGroups()
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

        public Vector2 DurationRange { get; set; } = new();
        public uint BoosterImplantID { get; set; }
        public BoosterImplantCategory ImplantCategory { get; set; }
        public List<BoosterImplantEffectTemplate> Effects { get; set; } = new();
        public List<List<BoosterImplantEffectTemplate>> RandomEffects { get; set; } = new();
        public List<uint> Conditions { get; set; } = new();
        public List<uint> RandomConditions { get; set; } = new();
    }
}
