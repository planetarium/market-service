namespace MarketService.GraphTypes;

public class QueryType : ObjectType<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor
            .Field(f => Query.GetProducts(default!))
            .Type<ListType<ProductModelType>>()
            .UsePaging<ProductModelType>()
            .UseProjection()
            .UseFiltering<ProductFilterType>()
            .UseSorting();

        descriptor
            .Field(f => Query.GetFavProducts(default!))
            .Type<ListType<FungibleAssetValueProductModelType>>()
            .UsePaging<FungibleAssetValueProductModelType>()
            .UseProjection()
            .UseFiltering<FungibleAssetValueProductFilterType>()
            .UseSorting();

        descriptor
            .Field(f => Query.GetItemProducts(default!))
            .Type<ListType<ItemProductModelType>>()
            .UsePaging<ItemProductModelType>()
            .UseProjection()
            .UseFiltering<ItemProductFilterType>()
            .UseSorting();
    }
}
