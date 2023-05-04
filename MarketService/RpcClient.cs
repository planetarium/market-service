using System.Collections.Concurrent;
using System.Diagnostics;
using Bencodex;
using Bencodex.Types;
using Grpc.Core;
using Grpc.Net.Client;
using Lib9c.Model.Order;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blocks;
using Libplanet.Crypto;
using MagicOnion.Client;
using MarketService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Battle;
using Nekoyume.Model.Item;
using Nekoyume.Model.Market;
using Nekoyume.Model.State;
using Nekoyume.Shared.Hubs;
using Nekoyume.Shared.Services;
using Nekoyume.TableData;
using Nekoyume.TableData.Crystal;
using Npgsql;

namespace MarketService;

public class RpcClient
{
    private const int MaxDegreeOfParallelism = 8;

    private static readonly List<ItemSubType> ShardedSubTypes = new()
    {
        ItemSubType.Weapon,
        ItemSubType.Armor,
        ItemSubType.Belt,
        ItemSubType.Necklace,
        ItemSubType.Ring,
        ItemSubType.Food,
        ItemSubType.Hourglass,
        ItemSubType.ApStone
    };

    private readonly Address _address;
    private readonly GrpcChannel _channel;
    private readonly Codec _codec = new();
    private readonly IDbContextFactory<MarketContext> _contextFactory;
    private readonly ILogger<RpcClient> _logger;
    private readonly Receiver _receiver;

    private readonly ParallelOptions _parallelOptions = new()
    {
        MaxDegreeOfParallelism = MaxDegreeOfParallelism
    };

    protected IBlockChainService Service = null!;


    public RpcClient(IOptions<RpcConfigOptions> options, ILogger<RpcClient> logger, Receiver receiver,
        IDbContextFactory<MarketContext> contextFactory)
    {
        _logger = logger;
        _address = new PrivateKey().ToAddress();
        var rpcConfigOptions = options.Value;
        _channel = GrpcChannel.ForAddress(
            $"http://{rpcConfigOptions.Host}:{rpcConfigOptions.Port}",
            new GrpcChannelOptions
            {
                Credentials = ChannelCredentials.Insecure,
                MaxReceiveMessageSize = null,
                HttpHandler = new SocketsHttpHandler
                {
                    EnableMultipleHttp2Connections = true,
                }
            }
        );
        _receiver = receiver;
        _contextFactory = contextFactory;
    }

    public bool Init { get; protected set; }

    public async Task StartAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            if (stoppingToken.IsCancellationRequested) stoppingToken.ThrowIfCancellationRequested();

            try
            {
                var hub = await StreamingHubClient.ConnectAsync<IActionEvaluationHub, IActionEvaluationHubReceiver>(
                    _channel, _receiver, cancellationToken: stoppingToken);
                _logger.LogDebug("Connected to hub");
                Service = MagicOnionClient.Create<IBlockChainService>(_channel).WithCancellationToken(stoppingToken);
                _logger.LogDebug("Connected to service");

                await hub.JoinAsync(_address.ToHex());
                await Service.AddClient(_address.ToByteArray());
                _logger.LogInformation("Joined to RPC headless");
                Init = true;

                _logger.LogDebug("Waiting for disconnecting");
                await hub.WaitForDisconnect();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error occurred");
            }
            finally
            {
                _logger.LogDebug("Retry to connect again");
            }
        }
    }

    public async Task<List<OrderDigest>> GetOrderDigests(ItemSubType itemSubType, byte[] hashBytes)
    {
        while (!Init) await Task.Delay(100);

        var orderDigestList = new List<OrderDigest>();
        try
        {
            var addressList = GetShopAddress(itemSubType);
            var result = await Service.GetStateBulk(addressList, hashBytes);
            var shopStates = GetShopStates(result);
            foreach (var shopState in shopStates)
            foreach (var orderDigest in shopState.OrderDigestList)
            {
                orderDigestList.Add(orderDigest);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unexpected exception occurred during SyncOrder: {Exc}", e);
        }

        return orderDigestList;
    }

    public async Task SyncOrder(byte[] hashBytes,
        CrystalEquipmentGrindingSheet crystalEquipmentGrindingSheet,
        CrystalMonsterCollectionMultiplierSheet crystalMonsterCollectionMultiplierSheet,
        CostumeStatSheet costumeStatSheet)
    {
        var sw = new Stopwatch();
        sw.Start();
        _logger.LogInformation("Start SyncOrder");
        var marketContext = await _contextFactory.CreateDbContextAsync();
        var productInfos = await marketContext.Products.AsNoTracking()
            .Select(p => new {p.ProductId, p.Exist, p.SellerAvatarAddress, p.Legacy}).ToListAsync();
        sw.Stop();
        _logger.LogDebug("Get Products: {Ts}", sw.Elapsed);
        sw.Restart();
        var existIds = new List<Guid>();
        var restoreIds = new ConcurrentBag<Guid>();
        var orderIds = new ConcurrentBag<Guid>();
        var avatarAddresses = new ConcurrentBag<Address>();
        foreach (var productInfo in productInfos)
        {
            if (productInfo.Legacy)
            {
                existIds.Add(productInfo.ProductId);
            }
            if (!avatarAddresses.Contains(productInfo.SellerAvatarAddress))
            {
                avatarAddresses.Add(productInfo.SellerAvatarAddress);
            }
        }

        if (!avatarAddresses.Any())
        {
            var itemSubTypes = new[]
            {
                ItemSubType.Weapon,
                ItemSubType.Armor,
                ItemSubType.Belt,
                ItemSubType.Necklace,
                ItemSubType.Ring,
                ItemSubType.Food,
                ItemSubType.Hourglass,
                ItemSubType.ApStone,
                ItemSubType.FullCostume,
                ItemSubType.HairCostume,
                ItemSubType.EarCostume,
                ItemSubType.EyeCostume,
                ItemSubType.TailCostume,
                ItemSubType.Title
            };

            var agentAddresses = new ConcurrentBag<Address>();
            await Parallel.ForEachAsync(itemSubTypes, _parallelOptions, async (itemSubType, st) =>
            {
                var list = await GetOrderDigests(itemSubType, hashBytes);

                foreach (var digest in list)
                {
                    var agentAddress = digest.SellerAgentAddress;
                    if (!agentAddresses.Contains(agentAddress))
                    {
                        agentAddresses.Add(agentAddress);
                    }
                }
            });

            var states = await GetStates(agentAddresses.Select(a => a.ToByteArray()).ToList(), hashBytes);
            Parallel.ForEach(states.Values, _parallelOptions, value =>
            {
                if (value is Dictionary d)
                {
                    var agentState = new AgentState(d);
                    foreach (var avatatarAddress in agentState.avatarAddresses.Values)
                    {
                        avatarAddresses.Add(avatatarAddress);
                    }
                }
            });
        }
        sw.Stop();
        _logger.LogDebug("Set existIds, avatarAddresses: {Ts}", sw.Elapsed);
        sw.Restart();

        var orderDigests = await GetOrderDigests(avatarAddresses.ToList(), hashBytes);
        sw.Stop();
        _logger.LogDebug("Get OrderDigests: {Ts}", sw.Elapsed);
        sw.Restart();
        var chainIds = orderDigests.Select(o => o.OrderId).ToList();
        var orderDigestList = orderDigests;
        Parallel.ForEach(chainIds, _parallelOptions, (orderId, _) =>
        {
            if (!existIds.Contains(orderId))
            {
                orderIds.Add(orderId);
            }
            else
            {
                var productInfo = productInfos.First(p => p.ProductId == orderId && p.Legacy);
                // Invalid state. restore product.
                if (!productInfo.Exist)
                {
                    restoreIds.Add(productInfo.ProductId);
                }
            }
        });
        sw.Stop();
        _logger.LogDebug("Set OrderIds, RestoreIds: {Ts}", sw.Elapsed);
        sw.Restart();

        var deletedIds = new ConcurrentBag<Guid>();
        Parallel.ForEach(existIds, _parallelOptions, (existId, _) =>
        {
            if (!chainIds.Contains(existId))
            {
                var productInfo = productInfos.FirstOrDefault(p => p.ProductId == existId && p.Exist);
                if (productInfo is not null)
                {
                    deletedIds.Add(existId);
                }
            }
        });
        sw.Stop();
        _logger.LogDebug("Set DeletedIds: {Ts}", sw.Elapsed);
        sw.Restart();

        var tradableIds = new List<Guid>();
        foreach (var digest in orderDigestList)
        {
            if (orderIds.Contains(digest.OrderId) && !tradableIds.Contains(digest.TradableId))
            {
                tradableIds.Add(digest.TradableId);
            }
        }
        sw.Stop();
        _logger.LogDebug("Set TradableIds: {Ts}", sw.Elapsed);
        sw.Restart();

        _logger.LogDebug("DeletedCounts: {Count}", deletedIds.Count);
        _logger.LogDebug("RestoreCounts: {Count}", restoreIds.Count());
        _logger.LogDebug("OrderCounts: {Count}", orderIds.Count());
        await InsertOrders(hashBytes, orderIds.ToList(), tradableIds, marketContext, orderDigestList,
            crystalEquipmentGrindingSheet, crystalMonsterCollectionMultiplierSheet, costumeStatSheet);
        sw.Stop();
        _logger.LogDebug("InsertOrders: {Ts}", sw.Elapsed);
        sw.Restart();

        await UpdateProducts(deletedIds.ToList(), marketContext, true);
        sw.Stop();
        _logger.LogDebug("DeleteProducts: {Ts}", sw.Elapsed);
        sw.Restart();
        await UpdateProducts(restoreIds.ToList(), marketContext, true, true);
        sw.Stop();
        _logger.LogDebug("RestoreProducts: {Ts}", sw.Elapsed);
    }

    public async Task UpdateProducts(List<Guid> deletedIds, MarketContext marketContext, bool legacy,
        bool exist = false)
    {
        // 등록취소, 판매된 경우 Exist 필드를 업데이트함. 
        if (deletedIds.Any())
        {
            await marketContext.Database.BeginTransactionAsync();
            var param = new NpgsqlParameter("@targetIds", deletedIds);
            await marketContext.Database.ExecuteSqlRawAsync(
                $"UPDATE products set exist = {exist} WHERE legacy = {legacy} and productid = any(@targetIds)",
                param);
            await marketContext.Database.CommitTransactionAsync();
        }
    }

    public async Task InsertOrders(byte[] hashBytes, List<Guid> orderIds, List<Guid> tradableIds,
        MarketContext marketContext, List<OrderDigest> orderDigestList,
        CrystalEquipmentGrindingSheet crystalEquipmentGrindingSheet,
        CrystalMonsterCollectionMultiplierSheet crystalMonsterCollectionMultiplierSheet,
        CostumeStatSheet costumeStatSheet)
    {
        if (orderIds.Any())
        {
            var orders = await GetOrders(orderIds, hashBytes);
            var items = await GetItems(tradableIds, hashBytes);
            var productBag = new ConcurrentBag<ProductModel>();
            orders
                .AsParallel()
                .WithDegreeOfParallelism(MaxDegreeOfParallelism)
                .ForAll(order =>
                {
                    var orderDigest = orderDigestList.First(o => o.OrderId == order.OrderId);
                    var item = items.OfType<ITradableItem>().First(i => i.TradableId == order.TradableId);
                    var itemProduct = new ItemProductModel
                    {
                        ProductId = order.OrderId,
                        SellerAgentAddress = order.SellerAgentAddress,
                        SellerAvatarAddress = order.SellerAvatarAddress,
                        Quantity = orderDigest.ItemCount,
                        ItemId = orderDigest.ItemId,
                        Price = decimal.Parse(orderDigest.Price.GetQuantityString()),
                        ItemType = item.ItemType,
                        ItemSubType = item.ItemSubType,
                        TradableId = item.TradableId,
                        RegisteredBlockIndex = orderDigest.StartedBlockIndex,
                        Exist = true,
                        Legacy = true,
                    };
                    itemProduct.Update(item, orderDigest.Price, costumeStatSheet, crystalEquipmentGrindingSheet,
                        crystalMonsterCollectionMultiplierSheet);
                    productBag.Add(itemProduct);
                });

            foreach (var chunk in productBag.Chunk(1000))
            {
                await marketContext.Products.AddRangeAsync(chunk);
                await marketContext.SaveChangesAsync();
            }
        }
    }

    public async Task SyncProduct(byte[] hashBytes, CrystalEquipmentGrindingSheet crystalEquipmentGrindingSheet,
        CrystalMonsterCollectionMultiplierSheet crystalMonsterCollectionMultiplierSheet,
        CostumeStatSheet costumeStatSheet)
    {
        while (!Init) await Task.Delay(100);

        try
        {
            var marketState = await GetMarket(hashBytes);
            var avatarAddressList = marketState.AvatarAddresses;
            var deletedIds = new List<Guid>();
            var chainIds = new List<Guid>();
            var products = new List<Product>();
            var productStates = await GetProductStates(avatarAddressList, hashBytes);
            var marketContext = await _contextFactory.CreateDbContextAsync();
            var existIds = marketContext.Database
                .SqlQueryRaw<Guid>(
                    $"Select productid from products where legacy = {false}")
                .ToList();
            foreach (var kv in productStates)
            {
                chainIds.Add(kv.Key);
                if (kv.Value.Equals(Null.Value)) deletedIds.Add(kv.Key);
                if (kv.Value is List deserialized && !existIds.Contains(kv.Key)) products.Add(ProductFactory.DeserializeProduct(deserialized));
            }

            // filter ids chain not exist product ids.
            deletedIds.AddRange(existIds.Where(i => !chainIds.Contains(i)));
            deletedIds = deletedIds.Distinct().ToList();
            await InsertProducts(products, costumeStatSheet, crystalEquipmentGrindingSheet,
                crystalMonsterCollectionMultiplierSheet);
            await UpdateProducts(deletedIds, marketContext, false);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unexpected exception occurred during SyncProduct: {Exc}", e);
        }
    }

    public async Task InsertProducts(List<Product> products, CostumeStatSheet costumeStatSheet,
        CrystalEquipmentGrindingSheet crystalEquipmentGrindingSheet,
        CrystalMonsterCollectionMultiplierSheet crystalMonsterCollectionMultiplierSheet)
    {
        var marketContext = await _contextFactory.CreateDbContextAsync();
        var itemProducts = products.OfType<ItemProduct>().ToList();
        var favProducts = products.OfType<FavProduct>().ToList();
        var productBag = new ConcurrentBag<ProductModel>();
        itemProducts
            .AsParallel()
            .WithDegreeOfParallelism(MaxDegreeOfParallelism)
            .ForAll(itemProduct =>
            {
                var item = (ItemBase) itemProduct.TradableItem;
                var itemProductModel = new ItemProductModel
                {
                    ProductId = itemProduct.ProductId,
                    SellerAgentAddress = itemProduct.SellerAgentAddress,
                    SellerAvatarAddress = itemProduct.SellerAvatarAddress,
                    Quantity = itemProduct.ItemCount,
                    ItemId = item.Id,
                    Price = decimal.Parse(itemProduct.Price.GetQuantityString()),
                    ItemType = item.ItemType,
                    ItemSubType = item.ItemSubType,
                    TradableId = itemProduct.TradableItem.TradableId,
                    RegisteredBlockIndex = itemProduct.RegisteredBlockIndex,
                    Exist = true,
                };

                itemProductModel.Update(itemProduct.TradableItem, itemProduct.Price, costumeStatSheet,
                    crystalEquipmentGrindingSheet, crystalMonsterCollectionMultiplierSheet);
                productBag.Add(itemProductModel);
            });

        favProducts
            .AsParallel()
            .WithDegreeOfParallelism(MaxDegreeOfParallelism)
            .ForAll(favProduct =>
            {
                var asset = favProduct.Asset;
                decimal price = decimal.Parse(favProduct.Price.GetQuantityString());
                decimal quantity = decimal.Parse(asset.GetQuantityString());
                var favProductModel = new FungibleAssetValueProductModel
                {
                    SellerAvatarAddress = favProduct.SellerAvatarAddress,
                    DecimalPlaces = asset.Currency.DecimalPlaces,
                    Exist = true,
                    Legacy = false,
                    Price = price,
                    ProductId = favProduct.ProductId,
                    Quantity = quantity,
                    RegisteredBlockIndex = favProduct.RegisteredBlockIndex,
                    SellerAgentAddress = favProduct.SellerAgentAddress,
                    Ticker = asset.Currency.Ticker,
                    UnitPrice = price / quantity,
                };
                productBag.Add(favProductModel);
            });

        await marketContext.Products.AddRangeAsync(productBag.ToList());
        await marketContext.SaveChangesAsync();
    }

    public async Task<T> GetSheet<T>(byte[] hashBytes) where T : ISheet, new()
    {
        var address = Addresses.GetSheetAddress<T>();
        var result = await Service.GetState(address.ToByteArray(), hashBytes);
        if (_codec.Decode(result) is Text t)
        {
            var sheet = new T();
            sheet.Set(t);
            return sheet;
        }

        throw new Exception();
    }

    public async Task<byte[]> GetBlockHashBytes()
    {
        var tipBytes = await Service.GetTip();
        var block =
            BlockMarshaler.UnmarshalBlock<PolymorphicAction<ActionBase>>((Dictionary) _codec.Decode(tipBytes));
        return block.Hash.ToByteArray();
    }

    public async Task<Dictionary<Guid, IValue>> GetProductStates(IEnumerable<Address> avatarAddressList,
        byte[] hashBytes)
    {
        var productListAddresses = avatarAddressList.Select(a => ProductsState.DeriveAddress(a).ToByteArray()).ToList();
        var productListResult = await GetChunkedStates(productListAddresses, hashBytes);
        var productLists = GetProductsState(productListResult);
        var productIdList = productLists.SelectMany(p => p.ProductIds).ToList();
        var productIds = new Dictionary<Address, Guid>();
        foreach (var productId in productIdList) productIds[Product.DeriveAddress(productId)] = productId;
        var productResult = await GetChunkedStates(productIds.Keys.Select(a => a.ToByteArray()).ToList(), hashBytes);
        var result = new Dictionary<Guid, IValue>();
        foreach (var kv in productResult)
        {
            var productId = productIds[kv.Key];
            result[productId] = kv.Value;
        }

        return result;
    }

    public List<ProductsState> GetProductsState(Dictionary<Address, IValue> queryResult)
    {
        var result = new List<ProductsState>();
        foreach (var kv in queryResult)
            if (kv.Value is List list)
                result.Add(new ProductsState(list));

        return result;
    }

    public IEnumerable<byte[]> GetShopAddress(ItemSubType itemSubType)
    {
        if (ShardedSubTypes.Contains(itemSubType))
        {
            var addressList = ShardedShopState.AddressKeys.Select(nonce =>
                ShardedShopStateV2.DeriveAddress(itemSubType, nonce).ToByteArray());
            return addressList;
        }

        return new[] {ShardedShopStateV2.DeriveAddress(itemSubType, "").ToByteArray()};
    }

    public IEnumerable<ShardedShopStateV2> GetShopStates(Dictionary<byte[], byte[]> queryResult)
    {
        var result = new List<ShardedShopStateV2>();
        foreach (var kv in queryResult)
        {
            var decode = _codec.Decode(kv.Value);
            if (decode is Dictionary dictionary) result.Add(new ShardedShopStateV2(dictionary));
        }

        return result;
    }

    public async Task<List<Order>> GetOrders(IEnumerable<Guid> orderIds, byte[] hashBytes)
    {
        var orderAddressList = orderIds.Select(i => Order.DeriveAddress(i).ToByteArray()).ToList();
        var chunks = orderAddressList
            .Select((x, i) => new {Index = i, Value = x})
            .GroupBy(x => x.Index / 1000)
            .Select(x => x.Select(v => v.Value).ToList())
            .ToList();

        var orderBag = new ConcurrentBag<Order>();
        await Parallel.ForEachAsync(chunks, _parallelOptions, async (chunk, token) =>
        {
            var orderResult = await GetStates(chunk, hashBytes);
            foreach (var kv in orderResult)
            {
                var order = OrderFactory.Deserialize((Dictionary) kv.Value);
                orderBag.Add(order);
            }
        });
        return orderBag.ToList();
    }

    public async Task<List<ItemBase>> GetItems(IEnumerable<Guid> tradableIds, byte[] hashBytes)
    {
        var itemAddressList = tradableIds.Select(i => Addresses.GetItemAddress(i).ToByteArray()).ToList();
        var chunks = itemAddressList
            .Select((x, i) => new {Index = i, Value = x})
            .GroupBy(x => x.Index / 1000)
            .Select(x => x.Select(v => v.Value).ToList())
            .ToList();
        var itemBag = new ConcurrentBag<ItemBase>();
        await Parallel.ForEachAsync(chunks, _parallelOptions, async (chunk, token) =>
        {
            var itemResult = await GetStates(chunk, hashBytes);
            foreach (var kv in itemResult)
            {
                var item = ItemFactory.Deserialize((Dictionary) kv.Value);
                itemBag.Add(item);
            }
        });
        return itemBag.ToList();
    }

    public async Task<Dictionary<Address, IValue>> GetStates(List<byte[]> addressList, byte[] hashBytes)
    {
        var result = new ConcurrentDictionary<Address, IValue>();
        var queryResult = await Service.GetStateBulk(addressList, hashBytes);
        queryResult
            .AsParallel()
            .WithDegreeOfParallelism(MaxDegreeOfParallelism)
            .ForAll(kv =>
            {
                result.TryAdd(new Address(kv.Key), _codec.Decode(kv.Value));
            });
        return result.ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    public async Task<Dictionary<Address, IValue>> GetChunkedStates(List<byte[]> addressList, byte[] hashBytes)
    {
        var result = new ConcurrentDictionary<Address, IValue>();
        var chunks = addressList
            .Select((x, i) => new {Index = i, Value = x})
            .GroupBy(x => x.Index / 1000)
            .Select(x => x.Select(v => v.Value).ToList())
            .ToList();
        await Parallel.ForEachAsync(chunks, _parallelOptions, async (chunk, token) =>
        {
            var queryResult = await Service.GetStateBulk(chunk, hashBytes);
            foreach (var kv in queryResult) result[new Address(kv.Key)] = _codec.Decode(kv.Value);
        });

        return result.ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    public async Task<MarketState> GetMarket(byte[] hashBytes)
    {
        var marketResult = await Service.GetState(Addresses.Market.ToByteArray(), hashBytes);
        var value = _codec.Decode(marketResult);
        if (value is List list) return new MarketState(list);

        return new MarketState();
    }

    public async Task<List<OrderDigest>> GetOrderDigests(List<Address> avatarAddresses, byte[] hashBytes)
    {
        var digestListStateAddresses =
            avatarAddresses.Select(a => OrderDigestListState.DeriveAddress(a).ToByteArray()).ToList();
        var digestListStateResult = await GetStates(digestListStateAddresses, hashBytes);
        var orderDigests = new ConcurrentBag<OrderDigest>();
        Parallel.ForEach(digestListStateResult.Values, _parallelOptions, value =>
        {
            if (value is Dictionary dictionary)
            {
                var digestListState = new OrderDigestListState(dictionary);
                foreach (var orderDigest in digestListState.OrderDigestList)
                {
                    if (orderDigest.StartedBlockIndex > ActionObsoleteConfig.V100080ObsoleteIndex)
                    {
                        orderDigests.Add(orderDigest);
                    }
                }
            }
        });
        return orderDigests.ToList();
    }
}
