using MarketService.Models;

namespace MarketService.GraphTypes;

public class ProductModelType : UnionType<ProductModel>
{
    protected override void Configure(IUnionTypeDescriptor descriptor)
    {
        descriptor.Type<FungibleAssetValueProductModelType>();
        descriptor.Type<ItemProductModelType>();
    }
}
