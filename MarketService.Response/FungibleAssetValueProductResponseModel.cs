using System;
using Libplanet;
using MarketService.Response.Interface;

namespace MarketService.Response;

[Serializable]
public class FungibleAssetValueProductResponseModel : IFungibleAssetValueProductSchema
{
    public Guid ProductId { get; set; }
    public Address SellerAgentAddress { get; set; }
    public Address SellerAvatarAddress { get; set; }
    public int Price { get; set; }
    public int Quantity { get; set; }
    public long RegisteredBlockIndex { get; set; }
    public bool Exist { get; set; }
    public bool Legacy { get; set; }
    public byte DecimalPlaces { get; set; }
    public string Ticker { get; set; }
}
