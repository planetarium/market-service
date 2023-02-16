using System;
using Libplanet;

namespace MarketService.Response.Interface;

public interface IProductSchema
{
    public Guid ProductId { get; set; }
    public Address SellerAgentAddress { get; set; }
    public Address SellerAvatarAddress { get; set; }
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    public long RegisteredBlockIndex { get; set; }
    public bool Exist { get; set; }
    public bool Legacy { get; set; }
}
