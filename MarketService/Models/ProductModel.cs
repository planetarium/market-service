using System.ComponentModel.DataAnnotations;
using Libplanet;
using MarketService.Response.Interface;
using Microsoft.EntityFrameworkCore;

namespace MarketService.Models;

[Index(nameof(Exist))]
public class ProductModel : IProductSchema
{
    [Key] public Guid ProductId { get; set; }

    public Address SellerAgentAddress { get; set; }
    public Address SellerAvatarAddress { get; set; }
    public int Price { get; set; }
    public int Quantity { get; set; }
    public long RegisteredBlockIndex { get; set; }
    public bool Exist { get; set; }
    public bool Legacy { get; set; }
}
