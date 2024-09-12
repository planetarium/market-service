using HotChocolate.Data.Filters;
using Libplanet.Crypto;
using MarketService.Models;

namespace MarketService.GraphTypes;

public class ProductFilterType : FilterInputType<ProductModel>
{
    protected override void Configure(IFilterInputTypeDescriptor<ProductModel> descriptor)
    {
        descriptor.Field(f => f.SellerAgentAddress).Type<CustomAddressOperationFilterInputType>();
        descriptor.Field(f => f.SellerAvatarAddress).Type<CustomAddressOperationFilterInputType>();
    }

    public class CustomAddressOperationFilterInputType : StringOperationFilterInputType
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(DefaultFilterOperations.Equals).Type<AddressType>();
        }
    }
}
