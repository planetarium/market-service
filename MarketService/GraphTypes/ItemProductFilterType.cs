using HotChocolate.Data.Filters;
using MarketService.Models;

namespace MarketService.GraphTypes;

public class ItemProductFilterType : FilterInputType<ItemProductModel>
{
    protected override void Configure(IFilterInputTypeDescriptor<ItemProductModel> descriptor)
    {
        descriptor.Field(f => f.SellerAgentAddress).Type<ProductFilterType.CustomAddressOperationFilterInputType>();
        descriptor.Field(f => f.SellerAvatarAddress).Type<ProductFilterType.CustomAddressOperationFilterInputType>();
    }
}
