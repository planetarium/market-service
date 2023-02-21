using Libplanet;
using MarketService.Models;
using MarketService.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Nekoyume.Model.Item;

namespace MarketService.Controllers;

[ApiController]
[Route("[controller]")]
public class MarketController : ControllerBase
{
    private readonly MarketContext _dbContext;
    private readonly ILogger<MarketController> _logger;
    private readonly IMemoryCache _memoryCache;

    public MarketController(ILogger<MarketController> logger, MarketContext marketContext, IMemoryCache memoryCache)
    {
        _logger = logger;
        _dbContext = marketContext;
        _memoryCache = memoryCache;
    }

    [HttpGet("products/items/{type}")]
    public async Task<MarketProductResponse> GetItemProducts(int type, int? limit, int? offset, string? order)
    {
        var itemSubType = (ItemSubType) type;
        var queryOffset = offset ?? 0;
        var queryLimit = limit ?? 100;
        var sort = string.IsNullOrEmpty(order) ? "cp_desc" : order;
        var queryResult = await Get(itemSubType, queryLimit, queryOffset, sort);
        var totalCount = queryResult.Count;
        return new MarketProductResponse(
            totalCount,
            queryLimit,
            queryOffset,
            queryResult
                .Skip(queryOffset)
                .Take(queryLimit));
    }

    private async Task<List<ItemProductModel>> Get(ItemSubType itemSubType, int queryLimit, int queryOffset, string sort)
    {
        var cacheKey = $"{itemSubType}_{queryLimit}_{queryOffset}_{sort}";
        if (!_memoryCache.TryGetValue(cacheKey, out List<ItemProductModel>? queryResult))
        {
            var query = _dbContext.ItemProducts
                .AsNoTracking()
                .Include(p => p.Skills)
                .Include(p => p.Stats)
                .Where(p => p.ItemSubType == itemSubType && p.Exist)
                .AsSingleQuery();
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
                "crystal_per_price_desc" => query.OrderByDescending(p => p.CrystalPerPrice),
                "crystal_per_price" => query.OrderBy(p => p.CrystalPerPrice),
                _ => query
            };
            var result = await query.ToListAsync();
            _memoryCache.Set(cacheKey, result, TimeSpan.FromMinutes(1f));
            queryResult = result;
        }

        return queryResult!;
    }

    [HttpGet("products/{address}")]
    public async Task<MarketProductResponse> GetItemProducts(string address)
    {
        if (!_memoryCache.TryGetValue(address, out List<ItemProductModel>? queryResult))
        {
            var avatarAddress = new Address(address);
            var query = await _dbContext.ItemProducts
                .AsNoTracking()
                .Include(p => p.Skills)
                .Include(p => p.Stats)
                .Where(p => p.SellerAvatarAddress.Equals(avatarAddress) && p.Exist)
                .OrderByDescending(p => p.RegisteredBlockIndex).ToListAsync();
            _memoryCache.Set(address, query, TimeSpan.FromMinutes(1f));
            queryResult = query;
        }
        return new MarketProductResponse(
            queryResult!.Count,
            0,
            0,
            queryResult);
    }
}
