using Libplanet;
using MarketService.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nekoyume.Model.Item;

namespace MarketService.Controllers;

[ApiController]
[Route("[controller]")]
public class MarketController : ControllerBase
{
    private readonly MarketContext _dbContext;
    private readonly ILogger<MarketController> _logger;

    public MarketController(ILogger<MarketController> logger, MarketContext marketContext)
    {
        _logger = logger;
        _dbContext = marketContext;
    }

    [HttpGet("products/items/{type}")]
    public MarketProductResponse GetItemProducts(int type, int? limit, int? offset, string? order)
    {
        var itemSubType = (ItemSubType) type;
        var queryOffset = offset ?? 0;
        var queryLimit = limit ?? 100;
        var sort = string.IsNullOrEmpty(order) ? "cp_desc" : order;
        var query = _dbContext.ItemProducts
            .AsNoTracking()
            .Where(p => p.ItemSubType == itemSubType && p.Exist);
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
        var totalCount = query.Count();
        return new MarketProductResponse(
            totalCount,
            queryLimit,
            queryOffset,
            query
                .Skip(queryOffset)
                .Take(queryLimit));
    }

    [HttpGet("products/{address}")]
    public MarketProductResponse GetItemProducts(string address)
    {
        var avatarAddress = new Address(address);
        var query = _dbContext.ItemProducts
            .AsNoTracking()
            .Where(p => p.SellerAvatarAddress.Equals(avatarAddress) && p.Exist)
            .OrderByDescending(p => p.RegisteredBlockIndex);
        return new MarketProductResponse(
            query.Count(),
            0,
            0,
            query);
    }
}
