using Libplanet;
using Libplanet.Crypto;
using MarketService.Controllers;
using MarketService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Xunit;

namespace MarketService.Tests;

public class MarketControllerTest
{
    [Fact]
    public void GetItemProducts()
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

        var context = new MarketContext(new DbContextOptionsBuilder<MarketContext>()
            .UseNpgsql($@"Host=localhost;Username=postgres;Database={GetType().Name}").UseLowerCaseNamingConvention()
            .Options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        context.Products.Add(product);
        Assert.True(product.Exist);
        context.SaveChanges();
        var controller = new MarketController(logger, context);
        var response = controller.GetItemProducts((int) ItemSubType.Armor, null, null, null);
        var result = Assert.Single(response.ItemProducts);
        Assert.IsType<ItemProductModel>(product);
        Assert.Equal(product.ProductId, result.ProductId);
        Assert.Equal(2, result.Quantity);
        Assert.Equal(3, result.Price);
        context.Database.EnsureDeleted();
    }
}
