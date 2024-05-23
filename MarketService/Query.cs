using MarketService.Models;
using Microsoft.EntityFrameworkCore;

namespace MarketService;

public class Query
{
    public static IQueryable<ProductModel> GetProducts([Service] MarketContext dbContext)
    {
        return dbContext.Products.AsNoTracking().AsQueryable();
    }

    public static IQueryable<FungibleAssetValueProductModel> GetFavProducts([Service] MarketContext dbContext)
    {
        return dbContext.FungibleAssetValueProducts.AsNoTracking().AsQueryable();
    }

    public static IQueryable<ItemProductModel> GetItemProducts([Service] MarketContext dbContext)
    {
        return dbContext.ItemProducts.AsNoTracking().AsQueryable();
    }
}
