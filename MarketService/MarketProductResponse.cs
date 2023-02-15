using MarketService.Models;

namespace MarketService;

public class MarketProductResponse
{
    public MarketProductResponse(int limit, int offset, int totalCount, IEnumerable<ProductModel> products)
    {
        Limit = limit;
        Offset = offset;
        TotalCount = totalCount;
        var list = products.ToList();
        ItemProducts = list.OfType<ItemProductModel>();
        FungibleAssetValueProducts = list.OfType<FungibleAssetValueProductModel>();
    }

    public int TotalCount { get; set; }
    public int Limit { get; set; }
    public int Offset { get; set; }
    public IEnumerable<ItemProductModel> ItemProducts { get; set; }
    public IEnumerable<FungibleAssetValueProductModel> FungibleAssetValueProducts { get; set; }
}
