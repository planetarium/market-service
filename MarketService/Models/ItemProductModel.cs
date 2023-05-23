using System.ComponentModel.DataAnnotations.Schema;
using Libplanet.Assets;
using MarketService.Response;
using MarketService.Response.Interface;
using Microsoft.EntityFrameworkCore;
using Nekoyume.Battle;
using Nekoyume.Helper;
using Nekoyume.Model;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;
using Nekoyume.TableData.Crystal;

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

    public int OptionCountFromCombination { get; set; }

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
            OptionCountFromCombination = OptionCountFromCombination,
            UnitPrice = UnitPrice,
        };
    }

    public void Update(ITradableItem tradableItem, FungibleAssetValue price, CostumeStatSheet costumeStatSheet,
        CrystalEquipmentGrindingSheet crystalEquipmentGrindingSheet,
        CrystalMonsterCollectionMultiplierSheet crystalMonsterCollectionMultiplierSheet)
    {
        var stats = new List<StatModel>();
#pragma warning disable CS0618
        CombatPoint = CPHelper.GetCP(tradableItem, costumeStatSheet);
#pragma warning restore CS0618
        UnitPrice = Price / Quantity;
        switch (tradableItem)
        {
            case ItemUsable itemUsable:
            {
                Grade = itemUsable.Grade;
                ElementalType = itemUsable.ElementalType;
                var map = itemUsable.StatsMap;
                var additionalStats = map.GetAdditionalStats(true).Select(s => new StatModel
                {
                    Additional = true,
                    Type = s.statType,
                    Value = s.additionalValue
                });
                var baseStats = map.GetBaseStats(true).Select(s => new StatModel
                {
                    Additional = false,
                    Type = s.statType,
                    Value = s.baseValue
                });
                stats.AddRange(additionalStats);
                stats.AddRange(baseStats);
                if (itemUsable is Equipment equipment)
                {
                    SetId = equipment.SetId;
                    Level = equipment.level;
                    OptionCountFromCombination = equipment.optionCountFromCombination;
                    var skillModels = new List<SkillModel>();
                    skillModels.AddRange(equipment.Skills.Select(s => new SkillModel
                    {
                        SkillId = s.SkillRow.Id,
                        Power = s.Power,
                        StatPowerRatio = s.StatPowerRatio,
                        Chance = s.Chance,
                        ReferencedStatType = s.ReferencedStatType,
                        ElementalType = s.SkillRow.ElementalType,
                        SkillCategory = s.SkillRow.SkillCategory,
                        HitCount = s.SkillRow.HitCount,
                        Cooldown = s.SkillRow.Cooldown
                    }));
                    skillModels.AddRange(equipment.BuffSkills.Select(s => new SkillModel
                    {
                        SkillId = s.SkillRow.Id,
                        Power = s.Power,
                        StatPowerRatio = s.StatPowerRatio,
                        Chance = s.Chance,
                        ReferencedStatType = s.ReferencedStatType,
                        ElementalType = s.SkillRow.ElementalType,
                        SkillCategory = s.SkillRow.SkillCategory,
                        HitCount = s.SkillRow.HitCount,
                        Cooldown = s.SkillRow.Cooldown
                    }));
                    Skills = skillModels;
                    try
                    {
                        var crystal = CrystalCalculator.CalculateCrystal(
                            new[] {equipment},
                            false,
                            crystalEquipmentGrindingSheet,
                            crystalMonsterCollectionMultiplierSheet,
                            0);
                        Crystal = (int) crystal.MajorUnit;
                        CrystalPerPrice = (int) crystal
                            .DivRem(price.MajorUnit).Quotient.MajorUnit;
                    }
                    catch (KeyNotFoundException)
                    {
                        CrystalPerPrice = 0;
                        Crystal = 0;
                    }
                }
                break;
            }
            case Costume costume:
            {
                Grade = costume.Grade;
                ElementalType = costume.ElementalType;
                var statsMap = new StatsMap();
                foreach (var row in costumeStatSheet.OrderedList!.Where(r => r.CostumeId == costume.Id))
                {
                    statsMap.AddStatValue(row.StatType, row.Stat);
                }
                var additionalStats = statsMap.GetBaseStats(true).Select(s => new StatModel
                {
                    Additional = false,
                    Type = s.statType,
                    Value = s.baseValue
                });
                stats.AddRange(additionalStats);
                break;
            }
        }

        if (stats.Any())
        {
            Stats = stats;
        }
    }
}
