using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using MarketService.Response.Interface;

namespace MarketService.Response
{
    [Serializable]
    public class MarketProductResponse
    {
        public int TotalCount { get; }
        public int Limit { get; }
        public int Offset { get; }
        public List<ItemProductResponseModel> ItemProducts { get; }
        public List<FungibleAssetValueProductResponseModel> FungibleAssetValueProducts { get; }

        public MarketProductResponse(int totalCount, int limit, int offset, IEnumerable<IProductSchema> products)
        {
            TotalCount = totalCount;
            Limit = limit;
            Offset = offset;
            var list = products.ToList();
            ItemProducts = list.OfType<IItemProductModel>().Select(p => p.ToResponse()).ToList();
            FungibleAssetValueProducts = list.OfType<IFungibleAssetValueProductModel>().Select(p => p.ToResponse()).ToList();
        }
        
        [JsonConstructor]
        public MarketProductResponse(int totalCount, int limit, int offset, List<ItemProductResponseModel> itemProducts, List<FungibleAssetValueProductResponseModel> fungibleAssetValueProducts)
        {
            TotalCount = totalCount;
            Limit = limit;
            Offset = offset;
            ItemProducts = itemProducts;
            FungibleAssetValueProducts = fungibleAssetValueProducts;
        }
    }
}
