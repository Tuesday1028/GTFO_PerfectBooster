using Clonesoft.Json;
using GameData;
using System.Reflection;
using TheArchive;
using static Hikaria.PerfectBooster.Features.PerfectBooster;

namespace Hikaria.PerfectBooster.Managers;

public static class BoosterImplantTemplateManager
{
    public static float BoosterPositiveEffectMultiplier { get; set; } = 1f;
    public static bool DisableBoosterConditions { get; set; } = false;
    public static bool DisableBoosterNegativeEffects { get; set; } = false;
    public static bool EnableBoosterTemplatePreference { get; set; } = false;
    public static bool EnableCustomBooster { get; set; } = false;

    private const string SettingsPath = "Settings";

    #region 强化剂自定义
    public static Dictionary<BoosterImplantCategory, List<CustomBoosterImplant>> CustomBoosterImplants { get; set; } = new();

    public static void CreateCustomBoosterImplantsFromInventory()
    {
        CustomBoosterImplants.Clear();
        CustomBoosterImplants = new()
            {
                { BoosterImplantCategory.Muted, new() },
                { BoosterImplantCategory.Bold, new() },
                { BoosterImplantCategory.Aggressive, new() }
            };
        foreach (var category in PersistentInventoryManager.Current.m_boosterImplantInventory.Categories)
        {
            foreach (var item in category.Inventory)
            {
                CustomBoosterImplants[item.Implant.Category].Add(new(item.Implant));
            }
        }
    }

    public static void LoadCustomBoosterImplantsFromSettings()
    {
        CustomBoosterImplants.Clear();

        string dir = Path.GetDirectoryName(Assembly.GetAssembly(typeof(BoosterImplantTemplateManager)).Location);
        string settingsPath = Path.Combine(dir, SettingsPath);
        if (!Directory.Exists(settingsPath))
        {
            Directory.CreateDirectory(settingsPath);
        }
        string fullPath = Path.Combine(settingsPath, CustomBoosterImplantsFile);
        if (!File.Exists(fullPath))
        {
            Dictionary<BoosterImplantCategory, List<CustomBoosterImplant>> data = new()
            {
                { BoosterImplantCategory.Muted, new() },
                { BoosterImplantCategory.Bold, new() },
                { BoosterImplantCategory.Aggressive, new() }
            };

            File.WriteAllText(fullPath, JsonConvert.SerializeObject(data, ArchiveMod.JsonSerializerSettings));
        }
        CustomBoosterImplants = JsonConvert.DeserializeObject<Dictionary<BoosterImplantCategory, List<CustomBoosterImplant>>>(File.ReadAllText(fullPath), ArchiveMod.JsonSerializerSettings)
            ?? new()
            {
                { BoosterImplantCategory.Muted, new() },
                { BoosterImplantCategory.Bold, new() },
                { BoosterImplantCategory.Aggressive, new() }
            };
    }

    public static void SaveCustomBoosterImplants()
    {
        string dir = Path.GetDirectoryName(Assembly.GetAssembly(typeof(BoosterImplantTemplateManager)).Location);
        string settingsPath = Path.Combine(dir, SettingsPath);
        if (!Directory.Exists(settingsPath))
        {
            Directory.CreateDirectory(settingsPath);
        }
        string fullPath = Path.Combine(settingsPath, CustomBoosterImplantsFile);
        File.WriteAllText(fullPath, JsonConvert.SerializeObject(CustomBoosterImplants, ArchiveMod.JsonSerializerSettings));
    }

    private const string CustomBoosterImplantsFile = "CustomBoosterImplants.json";

    public static void ApplyCustomBoosterImplants()
    {
        uint Id = 3223718U;
        for (int i = 0; i < 3; i++)
        {
            var inventory = PersistentInventoryManager.Current.m_boosterImplantInventory.Categories[i].Inventory;
            inventory.Clear();
            var category = (BoosterImplantCategory)i;
            for (int j = 0; j < CustomBoosterImplants[category].Count; j++)
            {
                var customBoosterImplant = CustomBoosterImplants[category][j];
                if (BoosterImplantTemplateDataBlock.GetBlock(customBoosterImplant.TemplateId) == null)
                {
                    continue;
                }
                List<DropServer.BoosterImplants.BoosterImplantEffect> effects = new();
                foreach (var effect in customBoosterImplant.Effects)
                {
                    effects.Add(new() { Id = effect.Id, Param = effect.Value });
                }
                var item = new BoosterImplantInventoryItem(new DropServer.BoosterImplants.BoosterImplantInventoryItem()
                {
                    Conditions = customBoosterImplant.Conditions.ToArray(),
                    Effects = effects.ToArray(),
                    Id = Id,
                    TemplateId = customBoosterImplant.TemplateId,
                    Flags = 1U,
                });
                inventory.Add(item);
                item.Implant.InstanceId = Id;
                item.Implant.Uses = (int)item.Implant.Template.DurationRange.y;
                customBoosterImplant.Name = item.Implant.GetCompositPublicName(true);
                Id++;
            }
        }
    }

    public class CustomBoosterImplant
    {
        public CustomBoosterImplant(BoosterImplant implant)
        {
            Name = implant.GetCompositPublicName(true);
            TemplateId = implant.TemplateId;
            Conditions = implant.Conditions.ToList();
            Category = implant.Category;
            Effects.Clear();
            foreach (var effect in implant.Effects)
            {
                Effects.Add(new(effect));
            }
        }

        public CustomBoosterImplant()
        {
        }

        public string Name { get; set; } = string.Empty;
        public BoosterImplantCategory Category { get; set; } = BoosterImplantCategory._COUNT;
        public uint TemplateId { get; set; } = 0;
        public List<uint> Conditions { get; set; } = new();
        public List<Effect> Effects { get; set; } = new();

        public class Effect
        {
            public Effect(BoosterImplant.Effect effect)
            {
                Id = effect.Id;
                Value = effect.Value;
            }

            public Effect()
            {
            }

            public uint Id { get; set; } = 0;

            public float Value { get; set; } = 1f;
        }
    }
    #endregion

    #region 完美强化剂
    public static void LoadTemplateData()
    {
        BoosterImplantTemplates.Clear();
        var templates = BoosterImplantTemplateDataBlock.GetAllBlocksForEditor();
        for (int i = 0; i < templates.Count; i++)
        {
            BoosterImplantTemplates.Add(new(templates[i]));
        }
    }

    public static void LoadTemplatePreferences()
    {
        BoosterTemplatePreferences.Clear();
        string dir = Path.GetDirectoryName(Assembly.GetAssembly(typeof(BoosterImplantTemplateManager)).Location);
        string settingsPath = Path.Combine(dir, SettingsPath);
        if (!Directory.Exists(settingsPath))
        {
            Directory.CreateDirectory(settingsPath);
        }
        string fullPath = Path.Combine(settingsPath, PreferenceFile);
        if (!File.Exists(fullPath))
        {
            List<BoosterImplantTemplatePreference> data = new();
            foreach (var template in BoosterImplantTemplates)
            {
                data.Add(new(template));
            }
            File.WriteAllText(fullPath, JsonConvert.SerializeObject(data, ArchiveMod.JsonSerializerSettings));
        }
        BoosterTemplatePreferences = JsonConvert.DeserializeObject<HashSet<BoosterImplantTemplatePreference>>(File.ReadAllText(fullPath), ArchiveMod.JsonSerializerSettings) ?? new();

        foreach (var template in BoosterImplantTemplates)
        {
            if (!BoosterTemplatePreferences.Any(p => p.TemplateId == template.BoosterImplantID && p.TemplateCategory == template.ImplantCategory))
            {
                BoosterTemplatePreferences.Add(new(template));
            }
            else
            {
                var pref = BoosterTemplatePreferences.FirstOrDefault(p => p.TemplateId == template.BoosterImplantID && p.TemplateCategory == template.ImplantCategory);
                BoosterTemplatePreferences.Remove(pref);
                BoosterTemplatePreferences.Add(new(template)
                {
                    EffectsGroupIndex = pref.EffectsGroupIndex,
                    ConditionsGroupIndex = pref.ConditionsGroupIndex
                });
            }
        }
    }

    public static void SaveTemplatePreferences()
    {
        string dir = Path.GetDirectoryName(Assembly.GetAssembly(typeof(BoosterImplantTemplateManager)).Location);
        string settingsPath = Path.Combine(dir, SettingsPath);
        if (!Directory.Exists(settingsPath))
        {
            Directory.CreateDirectory(settingsPath);
        }
        string fullPath = Path.Combine(settingsPath, PreferenceFile);
        File.WriteAllText(fullPath, JsonConvert.SerializeObject(BoosterTemplatePreferences, ArchiveMod.JsonSerializerSettings));
    }

    private const string PreferenceFile = "BoosterTemplatePreferences.json";

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
        var preference = BoosterTemplatePreferences.FirstOrDefault(p => p.TemplateId == boosterImplant.TemplateId && p.TemplateCategory == boosterImplant.Category);
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

    public static HashSet<BoosterImplantTemplate> BoosterImplantTemplates = new();

    public static HashSet<BoosterImplantTemplatePreference> BoosterTemplatePreferences = new();

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
    #endregion
}
