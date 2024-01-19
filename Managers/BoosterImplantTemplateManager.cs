using GameData;
using TheArchive.Core.ModulesAPI;
using static Hikaria.PerfectBooster.Features.PerfectBooster;

namespace Hikaria.PerfectBooster.Managers;

public static class BoosterImplantTemplateManager
{
    public static float BoosterPositiveEffectMultiplier { get; set; } = 1f;
    public static bool DisableBoosterConditions { get; set; } = false;
    public static bool DisableBoosterNegativeEffects { get; set; } = false;
    public static bool EnableBoosterTemplatePreference { get; set; } = false;
    public static bool EnableCustomeBooster { get; set; } = false;

    private static void LoadTemplateData()
    {
        BoosterImplantTemplates.Clear();
        var templates = BoosterImplantTemplateDataBlock.GetAllBlocksForEditor();
        for (int i = 0; i < templates.Count; i++)
        {
            BoosterImplantTemplates.Add(new(templates[i]));
        }
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

    public static bool TryGetBoosterImplantTemplatePreference(BoosterImplant boosterImplant, BoosterImplantTemplate template, out List<BoosterImplantEffectTemplate> effectGroup, out List<uint> conditions)
    {
        effectGroup = new();
        conditions = new();
        if (!EnableBoosterTemplatePreference)
        {
            return false;
        }
        var preference = BoosterTemplatePreferences.Value.FirstOrDefault(p => p.TemplateId == boosterImplant.TemplateId && p.TemplateCategory == boosterImplant.Category);
        if (preference == null || preference.TemplateId == 0)
        {
            return false;
        }
        bool findPreferedEffectGroup = false;
        bool findPreferedConditionsGroup = false;
        int effectGroupIndex = preference.EffectsGroupIndex;
        int conditionGroupIndex = preference.ConditionsGroupIndex;
        if (effectGroupIndex >= 0 && effectGroupIndex <= template.EffectGroups.Count)
        {
            effectGroup = template.EffectGroups[effectGroupIndex];
            findPreferedEffectGroup = true;
        }
        if (conditionGroupIndex >= 0 && conditionGroupIndex <= template.ConditionGroups.Count)
        {
            conditions = template.ConditionGroups[conditionGroupIndex];
            findPreferedConditionsGroup = true;
        }
        return findPreferedEffectGroup && findPreferedConditionsGroup;
    }

    public static bool TryGetBoosterImplantTemplate(BoosterImplant boosterImplant, out BoosterImplantTemplate template, out List<BoosterImplantEffectTemplate> effectGroup, out List<uint> conditionGroup)
    {
        effectGroup = new();
        conditionGroup = new();
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

    public static ModuleSetting<List<BoosterImplantTemplatePreference>> BoosterTemplatePreferences { get; set; } = new("BoosterTemplatePreferences", new(), () =>
    {
        if (!BoosterTemplatePreferences.Value.Any())
        {
            List<BoosterImplantTemplatePreference> data = new();
            foreach (var template in BoosterImplantTemplates)
            {
                data.Add(new(template));
            }
            BoosterTemplatePreferences.Value = data;
        }

        LoadTemplateData();

        foreach (var template in BoosterImplantTemplates)
        {
            if (!BoosterTemplatePreferences.Value.Any(p => p.TemplateId == template.BoosterImplantID && p.TemplateCategory == template.ImplantCategory))
            {
                BoosterTemplatePreferences.Value.Add(new(template));
            }
            else
            {
                var pref = BoosterTemplatePreferences.Value.FirstOrDefault(p => p.TemplateId == template.BoosterImplantID && p.TemplateCategory == template.ImplantCategory);
                BoosterTemplatePreferences.Value.Remove(pref);
                BoosterTemplatePreferences.Value.Add(new(template)
                {
                    EffectsGroupIndex = pref.EffectsGroupIndex,
                    ConditionsGroupIndex = pref.ConditionsGroupIndex
                });
            }
        }
    }, LoadingTime.AfterGameDataInited);

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
        public List<BoosterImplantEffectTemplate> Effects { get; set; } = new();
        public List<List<BoosterImplantEffectTemplate>> RandomEffects { get; set; } = new();
        public List<uint> Conditions { get; set; } = new();
        public List<uint> RandomConditions { get; set; } = new();


        public BoosterImplantTemplateDataBlock TemplateDataBlock { get; private set; } = null;

        public List<List<BoosterImplantEffectTemplate>> EffectGroups { get; set; } = new();
        public List<List<uint>> ConditionGroups { get; set; } = new();
    }
}
