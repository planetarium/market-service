using System;
using Libplanet.Crypto;
using MarketService.Response.Interface;

namespace MarketService.Response;

[Serializable]
public class FungibleAssetValueProductResponseModel : IFungibleAssetValueProductSchema
{
    public Guid ProductId { get; set; }
    public Address SellerAgentAddress { get; set; }
    public Address SellerAvatarAddress { get; set; }
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    public long RegisteredBlockIndex { get; set; }
    public bool Exist { get; set; }
    public bool Legacy { get; set; }
    public decimal UnitPrice { get; set; }
    public byte DecimalPlaces { get; set; }
    public string Ticker { get; set; }
}
