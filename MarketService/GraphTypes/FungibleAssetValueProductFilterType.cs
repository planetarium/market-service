using HotChocolate.Data.Filters;
using MarketService.Models;

namespace MarketService.GraphTypes;

public class FungibleAssetValueProductFilterType: FilterInputType<FungibleAssetValueProductModel>
{
    protected override void Configure(IFilterInputTypeDescriptor<FungibleAssetValueProductModel> descriptor)
    {
        descriptor.Field(f => f.SellerAgentAddress).Type<ProductFilterType.CustomAddressOperationFilterInputType>();
        descriptor.Field(f => f.SellerAvatarAddress).Type<ProductFilterType.CustomAddressOperationFilterInputType>();
    }
}
