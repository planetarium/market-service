namespace MarketService.Response.Interface;

public interface IFungibleAssetValueProductModel: IFungibleAssetValueProductSchema
{
    public FungibleAssetValueProductResponseModel ToResponse();
}
