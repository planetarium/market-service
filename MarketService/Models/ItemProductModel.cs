using System.ComponentModel.DataAnnotations.Schema;
using MarketService.Response;
using MarketService.Response.Interface;
using Nekoyume.Model;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Item;

namespace MarketService.Models;

public class ItemProductModel : ProductModel, IItemProductModel
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
    [NotMapped]
    public ICollection<SkillResponseModel> SkillModels => Skills.Select(s => s.ToResponse()).ToList();
    [NotMapped]
    public ICollection<StatResponseModel> StatModels => Stats.Select(s => s.ToResponse()).ToList();

    public ItemProductResponseModel ToResponse()
    {
        return new ItemProductResponseModel
        {
            ProductId = ProductId,
            SellerAgentAddress = SellerAgentAddress,
            SellerAvatarAddress = SellerAvatarAddress,
            Price = Price,
            Quantity = Quantity,
            RegisteredBlockIndex = RegisteredBlockIndex,
            Exist = Exist,
            Legacy = Legacy,
            ItemId = ItemId,
            Grade = Grade,
            ItemType = ItemType,
            ItemSubType = ItemSubType,
            ElementalType = ElementalType,
            TradableId = TradableId,
            SetId = SetId,
            CombatPoint = CombatPoint,
            Level = Level,
            SkillModels = Skills.OfType<ISkillModel>().Select(s => s.ToResponse()).ToList(),
            StatModels = Stats.OfType<IStatModel>().Select(s => s.ToResponse()).ToList(),
            Crystal = Crystal,
            CrystalPerPrice = CrystalPerPrice,
        };
    }
}
