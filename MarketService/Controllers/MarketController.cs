using Libplanet;
using Libplanet.Crypto;
using MarketService.Models;
using MarketService.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Npgsql;

namespace MarketService.Controllers;

[ApiController]
[Route("[controller]")]
public class MarketController : ControllerBase
{
    private readonly MarketContext _dbContext;
    private readonly ILogger<MarketController> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly TimeSpan _cacheTime = TimeSpan.FromSeconds(3f);

    public MarketController(ILogger<MarketController> logger, MarketContext marketContext, IMemoryCache memoryCache)
    {
        _logger = logger;
        _dbContext = marketContext;
        _memoryCache = memoryCache;
    }

    [HttpGet("products/items/{type}")]
    public async Task<MarketProductResponse> GetItemProducts(int type, int? limit, int? offset, string? order,
        string? stat, [FromQuery] int[] itemIds, [FromQuery] int[] iconIds, bool isCustom)
    {
        var itemSubType = (ItemSubType) type;
        var queryOffset = offset ?? 0;
        var queryLimit = limit ?? 100;
        var sort = string.IsNullOrEmpty(order) ? "cp_desc" : order;
        var statType = string.IsNullOrEmpty(stat) ? StatType.NONE : Enum.Parse<StatType>(stat, true);
        var queryResult = await Get(itemSubType, queryLimit, queryOffset, sort, statType, itemIds, iconIds, isCustom);
        var totalCount = queryResult.Count;
        return new MarketProductResponse(
            totalCount,
            queryLimit,
            queryOffset,
            queryResult
                .Skip(queryOffset)
                .Take(queryLimit));
    }

    /// <summary>
    /// Query and get filtered item list from market service.
    /// </summary>
    /// <param name="itemSubType">Item subtype to query.</param>
    /// <param name="queryLimit">Result count limit.</param>
    /// <param name="queryOffset">Offset to find from.</param>
    /// <param name="sort">Type of sorting result.</param>
    /// <param name="statType">Target stat type to query.</param>
    /// <param name="itemIds">Item IDs to filter. This will be ignored when `iconIds` comes in.</param>
    /// <param name="iconIds">Icon IDs to filter. This only works for equipment. This will be solely used when both IconIds and ItemIds are incoming.</param>
    /// <param name="isCustom">Flag to find from custom craft.</param>
    /// <returns></returns>
    private async Task<List<ItemProductModel>> Get(ItemSubType itemSubType, int queryLimit, int queryOffset,
        string sort, StatType statType, int[] itemIds, int[] iconIds, bool isCustom)
    {
        var iconIdKey = string.Join("_", iconIds.OrderBy(i => i));
        var iconCacheKey = $"{itemSubType}_{queryLimit}_{queryOffset}_{sort}_{statType}_{iconIdKey}_{isCustom}";
        var itemIdKey = string.Join("_", itemIds.OrderBy(i => i));
        var itemCacheKey = $"{itemSubType}_{queryLimit}_{queryOffset}_{sort}_{statType}_{itemIdKey}";

        if (_memoryCache.TryGetValue(iconIds.Any() ? iconCacheKey : itemCacheKey,
                out List<ItemProductModel>? queryResult))
            return queryResult!;

        var query = _dbContext.ItemProducts
            .AsNoTracking()
            .Include(p => p.Skills)
            .Include(p => p.Stats)
            .Where(p => p.ItemSubType == itemSubType && p.Exist);
        if (statType != StatType.NONE)
        {
            query = query.Where(p => p.Stats.Any(s => s.Type == statType));
        }

        // Use iconIds when present. Ignore itemIds in this case.
        if (iconIds.Any())
        {
            query = query.Where(p => iconIds.Contains(p.IconId));
        }
        else if (itemIds.Any())
        {
            query = query.Where(p => itemIds.Contains(p.ItemId));
        }

        if (isCustom)
        {
            query = query.Where(p => p.ByCustomCraft);
        }

        query = query.AsSingleQuery();
        query = sort switch
        {
            "cp_desc" => query.OrderByDescending(p => p.CombatPoint),
            "cp" => query.OrderBy(p => p.CombatPoint),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "price" => query.OrderBy(p => p.Price),
            "grade_desc" => query.OrderByDescending(p => p.Grade),
            "grade" => query.OrderBy(p => p.Grade),
            "crystal_desc" => query.OrderByDescending(p => p.Crystal),
            "crystal" => query.OrderBy(p => p.Crystal),
            "crystal_per_price_desc" => query.OrderByDescending(p => p.CrystalPerPrice)
                .ThenByDescending(p => p.Crystal),
            "crystal_per_price" => query.OrderBy(p => p.CrystalPerPrice).ThenBy(p => p.Crystal),
            "level_desc" => query.OrderByDescending(p => p.Level),
            "level" => query.OrderBy(p => p.Level),
            "opt_count_desc" => query.OrderByDescending(p => p.OptionCountFromCombination),
            "opt_count" => query.OrderBy(p => p.OptionCountFromCombination),
            "unit_price_desc" => query.OrderByDescending(p => p.UnitPrice).ThenByDescending(p => p.Price),
            "unit_price" => query.OrderBy(p => p.UnitPrice).ThenBy(p => p.Price),
            _ => query
        };
        var result = await query.ToListAsync();
        _memoryCache.Set(iconCacheKey, result, _cacheTime);
        queryResult = result;
        return queryResult!;
    }

    [HttpGet("products/{address}")]
    public async Task<MarketProductResponse> GetProducts(string address)
    {
        if (!_memoryCache.TryGetValue(address, out List<ProductModel>? queryResult))
        {
            var avatarAddress = new Address(address);
            var query = await _dbContext.Products
                .AsNoTracking()
                .Include(p => ((ItemProductModel)p).Skills)
                .Include(p => ((ItemProductModel)p).Stats)
                .Where(p => p.SellerAvatarAddress.Equals(avatarAddress) && p.Exist)
                .OrderByDescending(p => p.RegisteredBlockIndex)
                .AsSingleQuery()
                .ToListAsync();
            _memoryCache.Set(address, query, _cacheTime);
            queryResult = query;
        }
        return new MarketProductResponse(
            queryResult!.Count,
            0,
            0,
            queryResult);
    }

    [HttpGet("products/fav/{ticker}")]
    public async Task<MarketProductResponse> GetFavProducts(string ticker, int? limit, int? offset, string? order)
    {
        var queryOffset = offset ?? 0;
        var queryLimit = limit ?? 100;
        var sort = string.IsNullOrEmpty(order) ? "price_desc" : order;
        var cacheKey = $"{ticker}_{queryLimit}_{queryOffset}_{sort}";
        if (!_memoryCache.TryGetValue(cacheKey, out List<FungibleAssetValueProductModel>? queryResult))
        {
            var query = _dbContext.FungibleAssetValueProducts
                .AsNoTracking()
                .Where(p => p.Ticker.StartsWith(ticker) && p.Exist);
            query = sort switch
            {
                "price_desc" => query.OrderByDescending(p => p.Price),
                "price" => query.OrderBy(p => p.Price),
                "unit_price_desc" => query.OrderByDescending(p => p.UnitPrice),
                "unit_price" => query.OrderBy(p => p.UnitPrice),
                _ => query
            };
            queryResult = await query
                .AsSingleQuery()
                .ToListAsync();
            _memoryCache.Set(cacheKey, queryResult, _cacheTime);
        }
        return new MarketProductResponse(
            queryResult!.Count,
            0,
            0,
            queryResult
                .Skip(queryOffset)
                .Take(queryLimit));
    }

    [HttpGet("products/fav")]
    public async Task<MarketProductResponse> GetFavProducts([FromQuery] string[] tickers, int? limit, int? offset, string? order)
    {
        var queryOffset = offset ?? 0;
        var queryLimit = limit ?? 100;
        var sort = ReplaceSort(order);
        tickers = tickers.Select(t => $"%{t.ToUpper()}%").ToArray();
        var tickersKey = string.Join("_", tickers);
        var cacheKey = $"{tickersKey}_{queryLimit}_{queryOffset}_{sort}";
        if (!_memoryCache.TryGetValue(cacheKey, out List<FungibleAssetValueProductModel>? queryResult))
        {
            var param = new NpgsqlParameter("@tickers", tickers);
            var query = await _dbContext
                .FungibleAssetValueProducts
                .FromSqlRaw($"SELECT * FROM products WHERE exist = {true} AND ticker LIKE ANY(@tickers) ORDER BY {sort.Replace("_", " ")}", param)
                .ToListAsync();

            queryResult = query;
            _memoryCache.Set(cacheKey, queryResult, _cacheTime);
        }
        return new MarketProductResponse(
            queryResult!.Count,
            0,
            0,
            queryResult
                .Skip(queryOffset)
                .Take(queryLimit));
    }

    [HttpGet("products")]
    public async Task<MarketProductResponse> GetProductsById([FromQuery] Guid[] productIds)
    {
        var result = new List<ProductModel>();
        var filteredProductIds = new List<Guid>();
        foreach (var productId in productIds)
        {
            if (_memoryCache.TryGetValue(productId, out ProductModel? cached))
            {
                result.Add(cached!);
            }
            else
            {
                filteredProductIds.Add(productId);
            }
        }

        if (filteredProductIds.Any())
        {
            var query = await _dbContext.Products
                .AsNoTracking()
                .Include(p => ((ItemProductModel)p).Skills)
                .Include(p => ((ItemProductModel)p).Stats)
                .Where(p => filteredProductIds.Contains(p.ProductId))
                .OrderByDescending(p => p.RegisteredBlockIndex)
                .AsSingleQuery()
                .ToListAsync();
            foreach (var productModel in query)
            {
                _memoryCache.Set(productModel.ProductId, productModel, _cacheTime);
            }
            result.AddRange(query);
        }

        return new MarketProductResponse(result.Count, 0, 0, result);
    }

    public static string ReplaceSort(string order)
    {
        var sort = string.IsNullOrEmpty(order) ? "price_desc" : order;
        if (sort.Contains("unit_price"))
        {
            sort = sort.Replace("unit_price", "unitprice");
        }

        sort = sort.Replace("_", " ");
        return sort;
    }
}
