using System.ComponentModel.DataAnnotations;
using Libplanet;
using Microsoft.EntityFrameworkCore;

namespace MarketService.Models;

[Index(nameof(Exist))]
public class ProductModel
{
    [Key] public Guid ProductId { get; set; }

    public Address SellerAgentAddress { get; set; }
    public Address SellerAvatarAddress { get; set; }
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    public long RegisteredBlockIndex { get; set; }
    public bool Exist { get; set; }
    public bool Legacy { get; set; }
}