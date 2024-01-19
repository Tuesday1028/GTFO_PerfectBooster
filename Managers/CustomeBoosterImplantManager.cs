using GameData;
using TheArchive.Core.ModulesAPI;

namespace Hikaria.PerfectBooster.Managers
{
    public static class CustomeBoosterImplantManager
    {
        public static ModuleSetting<Dictionary<BoosterImplantCategory, List<CustomeBoosterImplant>>> CustomeBoosterImplants { get; set; } = new("CustomeBoosterImplants",
        new()
        {
            { BoosterImplantCategory.Muted, new() },
            { BoosterImplantCategory.Bold, new() },
            { BoosterImplantCategory.Aggressive, new() }
        });

        public static void CreateCustomeBoosterImplantsFromInventory()
        {
            CustomeBoosterImplants.Value = new()
            {
                { BoosterImplantCategory.Muted, new() },
                { BoosterImplantCategory.Bold, new() },
                { BoosterImplantCategory.Aggressive, new() }
            };
            foreach (var category in PersistentInventoryManager.Current.m_boosterImplantInventory.Categories)
            {
                foreach (var item in category.Inventory)
                {
                    CustomeBoosterImplants.Value[item.Implant.Category].Add(new(item.Implant));
                }
            }
        }

        public static void ApplyCustomeBoosterImplants()
        {
            uint Id = 3223718U;
            for (int i = 0; i < 3; i++)
            {
                var inventory = PersistentInventoryManager.Current.m_boosterImplantInventory.Categories[i].Inventory;
                inventory.Clear();
                var category = (BoosterImplantCategory)i;
                for (int j = 0; j < CustomeBoosterImplants.Value[category].Count; j++)
                {
                    var CustomeBoosterImplant = CustomeBoosterImplants.Value[category][j];
                    if (!CustomeBoosterImplant.Enabled)
                    {
                        continue;
                    }
                    if (BoosterImplantTemplateDataBlock.GetBlock(CustomeBoosterImplant.TemplateId) == null)
                    {
                        continue;
                    }
                    List<DropServer.BoosterImplants.BoosterImplantEffect> effects = new();
                    if (CustomeBoosterImplant.Effects.Any(p => p.Id == 0) || CustomeBoosterImplant.Conditions.Any(p => p == 0))
                    {
                        continue;
                    }
                    foreach (var effect in CustomeBoosterImplant.Effects)
                    {
                        effects.Add(new() { Id = effect.Id, Param = effect.Value });
                    }
                    var item = new BoosterImplantInventoryItem(new DropServer.BoosterImplants.BoosterImplantInventoryItem()
                    {
                        Conditions = CustomeBoosterImplant.Conditions.ToArray(),
                        Effects = effects.ToArray(),
                        Id = Id,
                        TemplateId = CustomeBoosterImplant.TemplateId,
                        Flags = 1U,
                    });
                    inventory.Add(item);
                    item.Implant.InstanceId = Id;
                    item.Implant.Uses = (int)item.Implant.Template.DurationRange.y;
                    CustomeBoosterImplant.Name = item.Implant.GetCompositPublicName(true);
                    Id++;
                }
            }
        }

        public class CustomeBoosterImplant
        {
            public CustomeBoosterImplant(BoosterImplant implant)
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

            public CustomeBoosterImplant()
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
