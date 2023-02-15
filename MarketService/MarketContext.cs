using Libplanet;
using MarketService.Models;
using Microsoft.EntityFrameworkCore;

namespace MarketService;

public class MarketContext : DbContext
{
    public MarketContext(DbContextOptions<MarketContext> options) : base(options)
    {
    }

    public DbSet<ProductModel> Products { get; set; }
    public DbSet<ItemProductModel> ItemProducts { get; set; }
    public DbSet<FungibleAssetValueProductModel> FungibleAssetValueProducts { get; set; }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<Address>()
            .HaveConversion<AddressConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductModel>()
            .HasDiscriminator<string>("product_type")
            .HasValue<ItemProductModel>("item")
            .HasValue<FungibleAssetValueProductModel>("fav");
        modelBuilder.Entity<ItemProductModel>()
            .OwnsMany(p => p.Skills, s =>
            {
                s.WithOwner().HasForeignKey("ProductId");
                s.Property<int>("Id");
                s.HasKey("Id");
            })
            .OwnsMany(p => p.Stats, s =>
            {
                s.WithOwner().HasForeignKey("ProductId");
                s.Property<int>("Id");
                s.HasKey("Id");
            });
    }
}