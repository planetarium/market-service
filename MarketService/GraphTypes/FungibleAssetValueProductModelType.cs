using MarketService.Models;

namespace MarketService.GraphTypes;

public class FungibleAssetValueProductModelType : ObjectType<FungibleAssetValueProductModel>
{
    protected override void Configure(IObjectTypeDescriptor<FungibleAssetValueProductModel> descriptor)
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
        descriptor.Field(f => f.DecimalPlaces)
            .Type<NonNullType<ByteType>>();
        descriptor.Field(f => f.Ticker)
            .Type<StringType>();
    }
}
