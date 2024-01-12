using Clonesoft.Json;
using GameData;
using System.Reflection;
using TheArchive;

namespace Hikaria.PerfectBooster.Managers
{
    public static class CustomBoosterImplantManager
    {
        private const string SettingsPath = "Settings";

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
                    if (!customBoosterImplant.Enabled)
                    {
                        continue;
                    }
                    if (BoosterImplantTemplateDataBlock.GetBlock(customBoosterImplant.TemplateId) == null)
                    {
                        continue;
                    }
                    List<DropServer.BoosterImplants.BoosterImplantEffect> effects = new();
                    if (customBoosterImplant.Effects.Any(p => p.Id == 0) || customBoosterImplant.Conditions.Any(p => p == 0))
                    {
                        continue;
                    }
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

            public bool Enabled { get; set; } = false;

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
    }
}
