using System.Collections.Generic;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Item;

namespace MarketService.Response.Interface;

public interface IItemProductSchema : IProductSchema
{
    public int ItemId { get; set; }
    public int IconId { get; set; }
    public int Grade { get; set; }
    public ItemType ItemType { get; set; }
    public ItemSubType ItemSubType { get; set; }
    public ElementalType ElementalType { get; set; }
    public int SetId { get; set; }
    public int CombatPoint { get; set; }
    public int Level { get; set; }
    public int Crystal { get; set; }
    public int CrystalPerPrice { get; set; }
    public ICollection<SkillResponseModel> SkillModels { get; }
    public ICollection<StatResponseModel> StatModels { get; }
    public int OptionCountFromCombination { get; set; }

    // Custom Crafted Equipment
    public bool ByCustomCraft { get; set; }
    public bool HasRandomOnlyIcon { get; set; }
}
