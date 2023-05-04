using MarketService.Response;
using MarketService.Response.Interface;

namespace MarketService.Models;

public class FungibleAssetValueProductModel : ProductModel, IFungibleAssetValueProductModel
{
    public byte DecimalPlaces { get; set; }
    public string Ticker { get; set; } = null!;

    public FungibleAssetValueProductResponseModel ToResponse()
    {
        return new FungibleAssetValueProductResponseModel
        {
            ProductId = ProductId,
            SellerAgentAddress = SellerAgentAddress,
            SellerAvatarAddress = SellerAvatarAddress,
            Price = Price,
            Quantity = Quantity,
            RegisteredBlockIndex = RegisteredBlockIndex,
            Exist = Exist,
            Legacy = Legacy,
            DecimalPlaces = DecimalPlaces,
            Ticker = Ticker,
            UnitPrice = UnitPrice,
        };
    }
}
