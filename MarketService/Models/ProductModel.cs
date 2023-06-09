using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Libplanet;
using MarketService.Response;
using MarketService.Response.Interface;
using Microsoft.EntityFrameworkCore;

namespace MarketService.Models;

[Index(nameof(Exist))]
public class ProductModel : IProductSchema
{
    [Key] public Guid ProductId { get; set; }

    public Address SellerAgentAddress { get; set; }
    public Address SellerAvatarAddress { get; set; }
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    public long RegisteredBlockIndex { get; set; }
    public bool Exist { get; set; }
    public bool Legacy { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Precision(18, 4)]
    public decimal UnitPrice { get; set; }
}
