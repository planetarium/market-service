using System;
using System.Collections.Generic;
using Libplanet;
using Libplanet.Crypto;
using MarketService.Response.Interface;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Item;

namespace MarketService.Response
{
    [Serializable]
    public class ItemProductResponseModel : IItemProductSchema
    {
        public Guid ProductId { get; set; }
        public Address SellerAgentAddress { get; set; }
        public Address SellerAvatarAddress { get; set; }
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public long RegisteredBlockIndex { get; set; }
        public bool Exist { get; set; }
        public bool Legacy { get; set; }
        public int ItemId { get; set; }
        public int? IconId { get; set; }
        public int Grade { get; set; }
        public ItemType ItemType { get; set; }
        public ItemSubType ItemSubType { get; set; }
        public ElementalType ElementalType { get; set; }
        public Guid TradableId { get; set; }
        public int SetId { get; set; }
        public int CombatPoint { get; set; }
        public int Level { get; set; }
        public ICollection<SkillResponseModel> SkillModels { get; set; }
        public ICollection<StatResponseModel> StatModels { get; set; }
        public int OptionCountFromCombination { get; set; }
        public decimal UnitPrice { get; set; }
        public int Crystal { get; set; }
        public int CrystalPerPrice { get; set; }
    }
}
