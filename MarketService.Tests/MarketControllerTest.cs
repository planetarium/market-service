using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Libplanet.Crypto;
using MarketService.Controllers;
using MarketService.Models;
using MarketService.Response;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Xunit;

namespace MarketService.Tests;

public class MarketControllerTest
{
    private readonly ILogger<MarketController> _logger;
    private readonly MarketContext _context;

    public MarketControllerTest()
    {
        _logger = new Logger<MarketController>(new LoggerFactory());
        var host = Environment.GetEnvironmentVariable("TEST_DB_HOST") ?? "localhost";
        var userName = Environment.GetEnvironmentVariable("TEST_DB_USER") ?? "postgres";
        var pw = Environment.GetEnvironmentVariable("TEST_DB_PW");
        var connectionString = $"Host={host};Username={userName};Database={GetType().Name};";
        if (!string.IsNullOrEmpty(pw))
        {
            connectionString += $"Password={pw};";
        }

        _context = new MarketContext(new DbContextOptionsBuilder<MarketContext>()
            .UseNpgsql(connectionString).UseLowerCaseNamingConvention()
            .Options);
    }

    [Fact]
    public async void GetItemProducts()
    {
        var productPrice = 3 * CrystalCalculator.CRYSTAL;
        ItemProductModel product = new ItemProductModel
        {
            SellerAgentAddress = new PrivateKey().Address,
            Quantity = 2,
            Price = decimal.Parse(productPrice.GetQuantityString()),
            SellerAvatarAddress = new PrivateKey().Address,
            ItemId = 3,
            Exist = true,
            ItemSubType = ItemSubType.Armor,
            OptionCountFromCombination = 1,
            UnitPrice = 3m / 2,
        };
        Assert.Equal(1.5m, product.UnitPrice);
        await _context.Database.EnsureDeletedAsync();
        await _context.Database.EnsureCreatedAsync();
        _context.Products.Add(product);
        Assert.True(product.Exist);
        await _context.SaveChangesAsync();
        var cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = null,
        });
        var controller = new MarketController(_logger, _context, cache);
        var response = await controller.GetItemProducts((int) ItemSubType.Armor, null, null, null, null, Array.Empty<int>(), Array.Empty<int>(), false);
        var result = Assert.Single(response.ItemProducts);
        Assert.IsType<ItemProductResponseModel>(result);
        Assert.Equal(product.ProductId, result.ProductId);
        Assert.Equal(2, result.Quantity);
        Assert.Equal(3, result.Price);
        Assert.Equal(1, result.OptionCountFromCombination);
        Assert.Equal(1.5m, result.UnitPrice);
       var json = JsonSerializer.Serialize(result);
        var des = JsonSerializer.Deserialize<ItemProductResponseModel>(json);
        Assert.NotNull(des);
        var json2 = JsonSerializer.Serialize(response);
        var des2 = JsonSerializer.Deserialize<MarketProductResponse>(json2);
        Assert.NotEmpty(des2!.ItemProducts);
        await _context.Database.EnsureDeletedAsync();
    }

    [Fact]
    public async void GetProductsById()
    {
        var productPrice = 3 * CrystalCalculator.CRYSTAL;
        ProductModel product = new ItemProductModel
        {
            SellerAgentAddress = new PrivateKey().Address,
            Quantity = 2,
            Price = decimal.Parse(productPrice.GetQuantityString()),
            SellerAvatarAddress = new PrivateKey().Address,
            ItemId = 3,
            Exist = true,
            ItemSubType = ItemSubType.Armor
        };

        ProductModel product2 = new ItemProductModel
        {
            SellerAgentAddress = new PrivateKey().Address,
            Quantity = 2,
            Price = decimal.Parse(productPrice.GetQuantityString()),
            SellerAvatarAddress = new PrivateKey().Address,
            ItemId = 3,
            Exist = true,
            ItemSubType = ItemSubType.Armor
        };
        await _context.Database.EnsureDeletedAsync();
        await _context.Database.EnsureCreatedAsync();
        _context.Products.Add(product);
        _context.Products.Add(product2);
        await _context.SaveChangesAsync();
        var cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = null,
        });
        var controller = new MarketController(_logger, _context, cache);
        var productIds = new List<Guid>
        {
            product.ProductId,
        };
        Assert.False(cache.TryGetValue(product.ProductId, out _));
        Assert.False(cache.TryGetValue(product2.ProductId, out _));

        var response = await controller.GetProductsById(productIds.ToArray());
        var result = Assert.Single(response.ItemProducts);
        Assert.True(cache.TryGetValue(product.ProductId, out ItemProductModel? cached));
        Assert.Equal(cached!.ProductId, product.ProductId);
        Assert.False(cache.TryGetValue(product2.ProductId, out _));

        productIds.Add(product2.ProductId);
        var response2 = await controller.GetProductsById(productIds.ToArray());
        Assert.Equal(2, response2.ItemProducts.Count);
        Assert.True(cache.TryGetValue(product.ProductId, out _));
        Assert.True(cache.TryGetValue(product2.ProductId, out ItemProductModel? cached2));
        Assert.Equal(cached2!.ProductId, product2.ProductId);
        await _context.Database.EnsureDeletedAsync();
    }

    [Theory]
    [InlineData(null, 5, 1)]
    [InlineData("price_desc", 5, 1)]
    [InlineData("price", 1, 5)]
    public async void GetProductsByTicker(string? sort, int firstPrice, int lastPrice)
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.Database.EnsureCreatedAsync();
        var runeTickers = new[]
        {
            "RUNESTONE_FENRIR1", "RUNESTONE_FENRIR2", "RUNESTONE_FENRIR3", RuneHelper.StakeRune.Ticker,
            RuneHelper.DailyRewardRune.Ticker
        };
        for (var index = 0; index < runeTickers.Length; index++)
        {
            var ticker = runeTickers[index];
            var product = new FungibleAssetValueProductModel
            {
                SellerAgentAddress = new PrivateKey().Address,
                Quantity = 2,
                Price = 1 + index,
                SellerAvatarAddress = new PrivateKey().Address,
                DecimalPlaces = 0,
                Exist = true,
                Ticker = ticker,
                Legacy = false,
                ProductId = Guid.NewGuid(),
                RegisteredBlockIndex = 1L,
            };
            _context.Products.Add(product);
        }

        await _context.SaveChangesAsync();
        var cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = null,
        });
        var controller = new MarketController(_logger, _context, cache);
        var response = await controller.GetFavProducts("RUNE", null, 0, sort);
        var favProducts = response.FungibleAssetValueProducts;
        Assert.Equal(runeTickers.Length, favProducts.Count);
        Assert.Equal(firstPrice, favProducts.First().Price);
        Assert.Equal(lastPrice, favProducts.Last().Price);
    }

    [Fact]
    public async void GetItemProductsByStat()
    {
        var productPrice = 3 * CrystalCalculator.CRYSTAL;
        ProductModel product = new ItemProductModel
        {
            SellerAgentAddress = new PrivateKey().Address,
            Quantity = 2,
            Price = decimal.Parse(productPrice.GetQuantityString()),
            SellerAvatarAddress = new PrivateKey().Address,
            ItemId = 3,
            Exist = true,
            ItemSubType = ItemSubType.Armor,
            Stats = new List<StatModel>
            {
                new()
                {
                    Additional = false,
                    Type = StatType.HP,
                    Value = 1
                },
                new()
                {
                    Additional = true,
                    Type = StatType.ATK,
                    Value = 1
                }
            }
        };


        await _context.Database.EnsureDeletedAsync();
        await _context.Database.EnsureCreatedAsync();
        _context.Products.Add(product);
        Assert.True(product.Exist);
        await _context.SaveChangesAsync();
        var cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = null,
        });
        var controller = new MarketController(_logger, _context, cache);
        foreach (var stat in new [] {"atk", "ATK", "HP", "hp", "Hp", "Atk", null })
        {
            var response = await controller.GetItemProducts((int) ItemSubType.Armor, null, null, null, stat, Array.Empty<int>(), Array.Empty<int>(), false);
            var result = Assert.Single(response.ItemProducts);
            Assert.IsType<ItemProductResponseModel>(result);
            Assert.Equal(product.ProductId, result.ProductId);
            Assert.Equal(2, result.Quantity);
            Assert.Equal(3, result.Price);
        }
        var response2 = await controller.GetItemProducts((int) ItemSubType.Armor, null, null, null, "DEF", Array.Empty<int>(), Array.Empty<int>(), false);
        Assert.Empty(response2.ItemProducts);
        await _context.Database.EnsureDeletedAsync();
    }

    [Theory]
    [InlineData("", "price desc")]
    [InlineData(null, "price desc")]
    [InlineData("price_desc", "price desc")]
    [InlineData("unit_price", "unitprice")]
    [InlineData("unit_price_desc", "unitprice desc")]
    public void ReplaceSort(string order, string expected)
    {
        Assert.Equal(expected, MarketController.ReplaceSort(order));
    }
}
