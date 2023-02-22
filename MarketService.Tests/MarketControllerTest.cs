using System;
using System.Text.Json;
using Libplanet;
using Libplanet.Crypto;
using MarketService.Controllers;
using MarketService.Models;
using MarketService.Response;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Xunit;

namespace MarketService.Tests;

public class MarketControllerTest
{
    [Fact]
    public async void GetItemProducts()
    {
        var logger = new Logger<MarketController>(new LoggerFactory());
        var productPrice = 3 * CrystalCalculator.CRYSTAL;
        ProductModel product = new ItemProductModel
        {
            SellerAgentAddress = new PrivateKey().ToAddress(),
            Quantity = 2,
            Price = decimal.Parse(productPrice.GetQuantityString()),
            SellerAvatarAddress = new PrivateKey().ToAddress(),
            ItemId = 3,
            Exist = true,
            ItemSubType = ItemSubType.Armor
        };

        var host = Environment.GetEnvironmentVariable("TEST_DB_HOST") ?? "localhost";
        var userName = Environment.GetEnvironmentVariable("TEST_DB_USER") ?? "postgres";
        var pw = Environment.GetEnvironmentVariable("TEST_DB_PW");
        var connectionString = $"Host={host};Username={userName};Database={GetType().Name};";
        if (!string.IsNullOrEmpty(pw))
        {
            connectionString += $"Password={pw};";
        }

        var context = new MarketContext(new DbContextOptionsBuilder<MarketContext>()
            .UseNpgsql(connectionString).UseLowerCaseNamingConvention()
            .Options);
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        context.Products.Add(product);
        Assert.True(product.Exist);
        await context.SaveChangesAsync();
        var cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = null,
        });
        var controller = new MarketController(logger, context, cache);
        var response = await controller.GetItemProducts((int) ItemSubType.Armor, null, null, null);
        var result = Assert.Single(response.ItemProducts);
        Assert.IsType<ItemProductResponseModel>(result);
        Assert.Equal(product.ProductId, result.ProductId);
        Assert.Equal(2, result.Quantity);
        Assert.Equal(3, result.Price);
       var json = JsonSerializer.Serialize(result);
        var des = JsonSerializer.Deserialize<ItemProductResponseModel>(json);
        Assert.NotNull(des);
        var json2 = JsonSerializer.Serialize(response);
        var des2 = JsonSerializer.Deserialize<MarketProductResponse>(json2);
        Assert.NotEmpty(des2.ItemProducts);
        await context.Database.EnsureDeletedAsync();
    }
}
