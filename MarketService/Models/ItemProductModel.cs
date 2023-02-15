using Nekoyume.Model.Elemental;
using Nekoyume.Model.Item;

namespace MarketService.Models;

public class ItemProductModel : ProductModel
{
    public int ItemId { get; set; }
    public int Grade { get; set; }
    public ItemType ItemType { get; set; }
    public ItemSubType ItemSubType { get; set; }
    public ElementalType ElementalType { get; set; }
    public Guid TradableId { get; set; }
    public int SetId { get; set; }
    public int CombatPoint { get; set; }
    public int Level { get; set; }
    public ICollection<SkillModel> Skills { get; set; }
    public ICollection<StatModel> Stats { get; set; }
    public int Crystal { get; set; }
    public int CrystalPerPrice { get; set; }
}
