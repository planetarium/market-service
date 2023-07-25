namespace MarketService.Response.Interface;

public interface IItemProductModel: IItemProductSchema
{
    public ItemProductResponseModel ToResponse();
}
