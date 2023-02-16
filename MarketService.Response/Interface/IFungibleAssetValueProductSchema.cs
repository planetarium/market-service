namespace MarketService.Response.Interface;

public interface IFungibleAssetValueProductSchema : IProductSchema
{
    public byte DecimalPlaces { get; set; }
    public string Ticker { get; set; }
}
