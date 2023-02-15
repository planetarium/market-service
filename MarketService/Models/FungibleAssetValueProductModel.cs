namespace MarketService.Models;

public class FungibleAssetValueProductModel : ProductModel
{
    public byte DecimalPlaces { get; set; }
    public string Ticker { get; set; } = null!;
}