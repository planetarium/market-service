using System;
using Libplanet;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Item;

namespace MarketService.Response.Interface;

public interface IItemProductModel: IItemProductSchema
{
    public ItemProductResponseModel ToResponse();
}
