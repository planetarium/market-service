using MarketService.Models;

namespace MarketService.GraphTypes;

public class ItemProductModelType : ObjectType<ItemProductModel>
{
    protected override void Configure(IObjectTypeDescriptor<ItemProductModel> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor
            .Field(f => f.ProductId)
            .Type<UuidType>();
        descriptor
            .Field(f => f.SellerAgentAddress)
            .Type<AddressType>();
        descriptor
            .Field(f => f.SellerAvatarAddress)
            .Type<AddressType>();
        descriptor
            .Field(f => f.Price)
            .Type<DecimalType>();
        descriptor
            .Field(f => f.Quantity)
            .Type<DecimalType>();
        descriptor
            .Field(f => f.RegisteredBlockIndex)
            .Type<LongType>();
        descriptor
            .Field(f => f.Exist)
            .Type<BooleanType>();
        descriptor
            .Field(f => f.Legacy)
            .Type<BooleanType>();
        descriptor
            .Field(f => f.CreatedAt)
            .Type<DateTimeType>();
        descriptor
            .Field(f => f.UnitPrice)
            .Type<DecimalType>();
        descriptor.Field(f => f.ItemId)
            .Type<NonNullType<IntType>>();
        descriptor.Field(f => f.Grade)
            .Type<NonNullType<IntType>>();
        descriptor
            .Field(f => f.CreatedAt)
            .Type<DateTimeType>();
        descriptor.Field(f => f.ItemType);
        descriptor.Field(f => f.ItemSubType);
        descriptor.Field(f => f.ElementalType);
        descriptor.Field(f => f.SetId);
        descriptor.Field(f => f.CombatPoint);
        descriptor.Field(f => f.Level);
        descriptor.Field(f => f.Crystal);
        descriptor.Field(f => f.CrystalPerPrice);
        descriptor.Field(f => f.OptionCountFromCombination);
        descriptor
            .Field(f => f.Skills)
            .Type<ListType<SkillModelType>>();
    }
}
