using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using Bencodex;
using Bencodex.Types;
using Grpc.Core;
using Grpc.Net.Client;
using Lib9c.Model.Order;
using Lib9c.Renderers;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Blocks;
using MagicOnion.Client;
using MarketService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Nekoyume;
using Nekoyume.Action;
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

    /// <summary>
    /// <see cref="ItemSubType"/> for <see cref="SyncOrder"/>
    /// </summary>
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
    private bool _ready;
    private bool _selfDisconnect;

    private readonly ParallelOptions _parallelOptions = new()
    {
        MaxDegreeOfParallelism = MaxDegreeOfParallelism
    };

    public IBlockChainService Service = null!;
    private IActionEvaluationHub _hub;

    public bool Ready => _ready;
    public Block Tip => _receiver.Tip;
    public Block PreviousTip => _receiver.PreviousTip;

    private readonly ActionRenderer _actionRenderer;

    public RpcClient(IOptions<RpcConfigOptions> options, ILogger<RpcClient> logger, Receiver receiver,
        IDbContextFactory<MarketContext> contextFactory, ActionRenderer actionRenderer)
    {
        _logger = logger;
        _address = new PrivateKey().Address;
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
                    KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                    KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                }
            }
        );
        _receiver = receiver;
        _contextFactory = contextFactory;
        _actionRenderer = actionRenderer;
        _actionRenderer.ActionRenderSubject.Subscribe(RenderAction);
    }

    /// <summary>
    /// Insert or Update <see cref="ProductModel"/> by Market related actions.
    /// </summary>
    /// <param name="ev"></param>
    public async void RenderAction(ActionEvaluation<ActionBase> ev)
    {
        if (ev.Exception is null)
        {
            var seed = ev.RandomSeed;
            var random = new LocalRandom(seed);
            var stateRootHash = ev.OutputState;
            var hashBytes = stateRootHash.ToByteArray();
            switch (ev.Action)
            {
                // Insert new product
                case RegisterProduct registerProduct:
                {
                    var crystalEquipmentGrindingSheet = await GetSheet<CrystalEquipmentGrindingSheet>(hashBytes);
                    var crystalMonsterCollectionMultiplierSheet =
                        await GetSheet<CrystalMonsterCollectionMultiplierSheet>(hashBytes);
                    var costumeStatSheet = await GetSheet<CostumeStatSheet>(hashBytes);
                    var products = new List<Product>();
                    var productIds = registerProduct.RegisterInfos.Select(_ => random.GenerateRandomGuid()).ToList();
                    var states = await GetProductStates(productIds, hashBytes);
                    foreach (var kv in states)
                    {
                        if (kv.Value is List deserialized)
                        {
                            products.Add(ProductFactory.DeserializeProduct(deserialized));
                        }
                    }

                    await InsertProducts(products, costumeStatSheet, crystalEquipmentGrindingSheet, crystalMonsterCollectionMultiplierSheet);
                    break;
                }
                // delete product
                case BuyProduct buyProduct:
                {
                    var deletedIds = new List<Guid>();
                    foreach (var productInfo in buyProduct.ProductInfos)
                    {
                        deletedIds.Add(productInfo.ProductId);
                    }

                    var marketContext = await _contextFactory.CreateDbContextAsync();
                    await DeleteProducts(deletedIds, marketContext);
                    break;
                }
                case CancelProductRegistration cancelProductRegistration:
                {
                    var deletedIds = new List<Guid>();
                    foreach (var productInfo in cancelProductRegistration.ProductInfos)
                    {
                        deletedIds.Add(productInfo.ProductId);
                    }

                    var marketContext = await _contextFactory.CreateDbContextAsync();
                    await DeleteProducts(deletedIds, marketContext);
                    break;
                }
                // Insert new product and delete product
                case ReRegisterProduct reRegisterProduct:
                {
                    var productIds = new List<Guid>();
                    var deletedIds = new List<Guid>();
                    foreach (var (productInfo, _) in reRegisterProduct.ReRegisterInfos)
                    {
                        deletedIds.Add(productInfo.ProductId);
                        productIds.Add(random.GenerateRandomGuid());
                    }
                    var crystalEquipmentGrindingSheet = await GetSheet<CrystalEquipmentGrindingSheet>(hashBytes);
                    var crystalMonsterCollectionMultiplierSheet =
                        await GetSheet<CrystalMonsterCollectionMultiplierSheet>(hashBytes);
                    var costumeStatSheet = await GetSheet<CostumeStatSheet>(hashBytes);
                    var products = new List<Product>();
                    var states = await GetProductStates(productIds, hashBytes);
                    foreach (var kv in states)
                    {
                        // check db all product ids avoid already synced products
                        if (kv.Value is List deserialized)
                        {
                            products.Add(ProductFactory.DeserializeProduct(deserialized));
                        }
                    }

                    await InsertProducts(products, costumeStatSheet, crystalEquipmentGrindingSheet, crystalMonsterCollectionMultiplierSheet);
                    var marketContext = await _contextFactory.CreateDbContextAsync();
                    await DeleteProducts(deletedIds, marketContext);
                    break;
                }
            }
        }
    }

    public async Task StartAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                _selfDisconnect = true;
                stoppingToken.ThrowIfCancellationRequested();
            }

            try
            {
                await Join(stoppingToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error occurred");
                _ready = false;
            }
            if (_selfDisconnect)
            {
                _logger.LogInformation("self disconnect");
                break;
            }
        }
    }

    private async Task Join(CancellationToken stoppingToken)
    {
        _hub = await StreamingHubClient.ConnectAsync<IActionEvaluationHub, IActionEvaluationHubReceiver>(
            _channel, _receiver, cancellationToken: stoppingToken);
        _logger.LogDebug("Connected to hub");
        Service = MagicOnionClient.Create<IBlockChainService>(_channel)
            .WithCancellationToken(stoppingToken);
        _logger.LogDebug("Connected to service");

        await _hub.JoinAsync(_address.ToHex());
        await Service.AddClient(_address.ToByteArray());
        _logger.LogInformation("Joined to RPC headless");
        _ready = true;

        _logger.LogDebug("Waiting for disconnecting");
        await _hub.WaitForDisconnect();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _selfDisconnect = true;
        await _hub.LeaveAsync();
    }

    /// <summary>
    /// Get <see cref="List{T}"/> of <see cref="OrderDigest"/> for get registered agent addresses
    /// </summary>
    /// <param name="itemSubType"></param>
    /// <param name="hashBytes"></param>
    /// <returns></returns>
    public async Task<List<OrderDigest>> GetOrderDigests(ItemSubType itemSubType, byte[] hashBytes)
    {
        while (Tip is null) await Task.Delay(100);

        var orderDigestList = new List<OrderDigest>();
        try
        {
            var addressList = GetShopAddress(itemSubType);
            var result =
                await Service.GetBulkStateByStateRootHash(hashBytes, ReservedAddresses.LegacyAccount.ToByteArray(), addressList);
            var shopStates = DeserializeShopStates(result);
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

    /// <summary>
    /// Insert and Update <see cref="ProductModel"/> from <see cref="Order"/>
    /// </summary>
    /// <param name="hashBytes">byte array from <see cref="Block.StateRootHash"/></param>
    /// <param name="crystalEquipmentGrindingSheet"><see cref="CrystalEquipmentGrindingSheet"/></param>
    /// <param name="crystalMonsterCollectionMultiplierSheet"><see cref="CrystalMonsterCollectionMultiplierSheet"/></param>
    /// <param name="costumeStatSheet"><see cref="CostumeStatSheet"/></param>
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
                ItemSubType.Title,
                // Currently shop is not managed by "order", so don't need to add new types in here
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

            var agentStates = await GetAgentStates(
                hashBytes,
                agentAddresses.Select(a => a.ToByteArray()).ToList());

            Parallel.ForEach(agentStates.Values, _parallelOptions, value =>
            {
                if (value is AgentState agentState)
                {
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

        await DeleteProducts(deletedIds.ToList(), marketContext);
    }

    /// <summary>
    /// Set <see cref="ProductModel"/> exist = false
    /// </summary>
    /// <param name="deletedIds"></param>
    /// <param name="marketContext"></param>
    /// <param name="legacy"></param>
    /// <param name="exist"></param>
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

    /// <summary>
    /// Create <see cref="ProductModel"/> from <see cref="Order"/>
    /// </summary>
    /// <param name="hashBytes">byte array from <see cref="Block.StateRootHash"/></param>
    /// <param name="crystalEquipmentGrindingSheet"><see cref="CrystalEquipmentGrindingSheet"/></param>
    /// <param name="crystalMonsterCollectionMultiplierSheet"><see cref="CrystalMonsterCollectionMultiplierSheet"/></param>
    /// <param name="costumeStatSheet"><see cref="CostumeStatSheet"/></param>
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
                    var item = items.First(i => i.TradableId == order.TradableId);
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

    /// <summary>
    /// Insert and Update ProductModel from <see cref="Product"/>
    /// </summary>
    /// <param name="hashBytes">byte array from <see cref="Block.StateRootHash"/></param>
    /// <param name="crystalEquipmentGrindingSheet"><see cref="CrystalEquipmentGrindingSheet"/></param>
    /// <param name="crystalMonsterCollectionMultiplierSheet"><see cref="CrystalMonsterCollectionMultiplierSheet"/></param>
    /// <param name="costumeStatSheet"><see cref="CostumeStatSheet"/></param>
    public async Task SyncProduct(byte[] hashBytes, CrystalEquipmentGrindingSheet crystalEquipmentGrindingSheet,
        CrystalMonsterCollectionMultiplierSheet crystalMonsterCollectionMultiplierSheet,
        CostumeStatSheet costumeStatSheet)
    {
        while (Tip is null) await Task.Delay(100);
        try
        {
            var sw = new Stopwatch();
            sw.Start();
            var marketState = await GetMarket(hashBytes);
            var avatarAddressList = marketState.AvatarAddresses;
            var products = new List<Product>();
            var marketContext = await _contextFactory.CreateDbContextAsync();
            var productInfos = await marketContext.Products
                .AsNoTracking()
                .Where(p => !p.Legacy)
                .Select(p => new {p.ProductId, p.Exist})
                .ToListAsync();
            var existIds = new List<Guid>();
            var dbIds = new List<Guid>();
            foreach (var productInfo in productInfos)
            {
                var productId = productInfo.ProductId;
                dbIds.Add(productInfo.ProductId);
                if (productInfo.Exist)
                {
                    existIds.Add(productId);
                }
            }
            var productListAddresses = avatarAddressList.Select(a => ProductsState.DeriveAddress(a).ToByteArray()).ToList();
            sw.Stop();
            _logger.LogDebug("[ProductWorker]Prepare existIds: {Elapsed}", sw.Elapsed);
            sw.Restart();
            var productListResult =
                await GetChunkedStates(hashBytes, ReservedAddresses.LegacyAccount.ToByteArray(), productListAddresses);
            sw.Stop();
            _logger.LogDebug("[ProductWorker]Get ChunkedStates: {Elapsed}", sw.Elapsed);
            sw.Restart();
            var productLists = DeserializeProductsState(productListResult);
            sw.Stop();
            _logger.LogDebug("[ProductWorker]Get ProductsState: {Elapsed}", sw.Elapsed);
            sw.Restart();
            var chainIds = productLists.SelectMany(p => p.ProductIds).ToList();
            var targetIds = chainIds.Except(dbIds).ToList();
            sw.Stop();
            _logger.LogDebug("[ProductWorker]Get Ids(Chain:{ChainCount}/Target:{TargetCount}): {Elapsed}", chainIds.Count, targetIds.Count, sw.Elapsed);
            sw.Restart();
            var productStates = await GetProductStates(targetIds, hashBytes);
            foreach (var kv in productStates)
            {
                // check db all product ids avoid already synced products
                if (kv.Value is List deserialized && !dbIds.Contains(kv.Key))
                {
                    products.Add(ProductFactory.DeserializeProduct(deserialized));
                }
            }
            sw.Stop();
            _logger.LogDebug("[ProductWorker]Get ProductStates({ProductCount}): {Elapsed}", products.Count, sw.Elapsed);
            sw.Restart();

            // filter ids chain not exist product ids.
            var deletedIds = existIds.Except(chainIds).Distinct().ToList();
            sw.Stop();
            _logger.LogDebug("[ProductWorker]distinct DeletedIds({DeletedCount}): {Elapsed}", deletedIds.Count, sw.Elapsed);
            sw.Restart();
            await InsertProducts(products, costumeStatSheet, crystalEquipmentGrindingSheet,
                crystalMonsterCollectionMultiplierSheet);
            sw.Stop();
            _logger.LogDebug("[ProductWorker]Insert Products: {Elapsed}", sw.Elapsed);
            sw.Restart();
            await DeleteProducts(deletedIds, marketContext);
            sw.Stop();
            _logger.LogDebug("[ProductWorker]Update Products: {Elapsed}", sw.Elapsed);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unexpected exception occurred during SyncProduct: {Exc}", e);
        }
    }

    private async Task<Dictionary<Guid, IValue>> GetProductStates(List<Guid> productIdList, byte[] hashBytes)
    {
        var productIds = new Dictionary<Address, Guid>();
        foreach (var productId in productIdList) productIds[Product.DeriveAddress(productId)] = productId;
        var productResult = await GetChunkedStates(
            hashBytes,
            ReservedAddresses.LegacyAccount.ToByteArray(),
            productIds.Keys.Select(a => a.ToByteArray()).ToList());
        var result = new Dictionary<Guid, IValue>();
        foreach (var kv in productResult)
        {
            var productId = productIds[kv.Key];
            result[productId] = kv.Value;
        }
        return result;
    }

    /// <summary>
    /// Insert <see cref="ProductModel"/> from <see cref="Product"/>
    /// </summary>
    /// <param name="products">List of <see cref="Product"/></param>
    /// <param name="crystalEquipmentGrindingSheet"><see cref="CrystalEquipmentGrindingSheet"/></param>
    /// <param name="crystalMonsterCollectionMultiplierSheet"><see cref="CrystalMonsterCollectionMultiplierSheet"/></param>
    /// <param name="costumeStatSheet"><see cref="CostumeStatSheet"/></param>
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
        var result = await Service.GetStateByStateRootHash(
            hashBytes,
            ReservedAddresses.LegacyAccount.ToByteArray(),
            address.ToByteArray());
        if (DeCompressState(result) is Text t)
        {
            var sheet = new T();
            sheet.Set(t);
            return sheet;
        }

        throw new Exception();
    }

    public async Task<byte[]> GetBlockStateRootHashBytes()
    {
        while (Tip is null)
        {
            await Task.Delay(1000);
        }
        return _receiver.Tip.StateRootHash.ToByteArray();
    }

    public List<ProductsState> DeserializeProductsState(Dictionary<Address, IValue> queryResult)
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

    /// <summary>
    /// Get <see cref="IEnumerable{T}"/> of <see cref="ShardedShopStateV2"/> for listing <see cref="OrderDigest"/>
    /// </summary>
    /// <param name="queryResult"></param>
    /// <returns></returns>
    public IEnumerable<ShardedShopStateV2> DeserializeShopStates(Dictionary<byte[], byte[]> queryResult)
    {
        var result = new List<ShardedShopStateV2>();
        foreach (var kv in queryResult)
        {
            var decode = DeCompressState(kv.Value);
            if (decode is Dictionary dictionary) result.Add(new ShardedShopStateV2(dictionary));
        }

        return result;
    }

    /// <summary>
    /// Get <see cref="List{T}"/> of <see cref="Order"/> for <see cref="ItemProductModel"/>.
    /// </summary>
    /// <seealso cref="StatModel"/>
    /// <seealso cref="SkillModel"/>
    /// <param name="orderIds"></param>
    /// <param name="hashBytes"></param>
    /// <returns></returns>
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
            var orderResult = await GetStates(hashBytes, ReservedAddresses.LegacyAccount.ToByteArray(), chunk);
            foreach (var kv in orderResult)
            {
                var order = OrderFactory.Deserialize((Dictionary) kv.Value);
                orderBag.Add(order);
            }
        });
        return orderBag.ToList();
    }

    /// <summary>
    /// Get <see cref="List{T}"/> of <see cref="ITradableItem"/> for <see cref="ItemProductModel"/>.
    /// </summary>
    /// <seealso cref="StatModel"/>
    /// <seealso cref="SkillModel"/>
    /// <param name="tradableIds"></param>
    /// <param name="hashBytes"></param>
    /// <returns></returns>
    public async Task<List<ITradableItem>> GetItems(IEnumerable<Guid> tradableIds, byte[] hashBytes)
    {
        var itemAddressList = tradableIds.Select(i => Addresses.GetItemAddress(i).ToByteArray()).ToList();
        var chunks = itemAddressList
            .Select((x, i) => new {Index = i, Value = x})
            .GroupBy(x => x.Index / 1000)
            .Select(x => x.Select(v => v.Value).ToList())
            .ToList();
        var itemBag = new ConcurrentBag<ITradableItem>();
        await Parallel.ForEachAsync(chunks, _parallelOptions, async (chunk, token) =>
        {
            var itemResult = await GetStates(hashBytes, ReservedAddresses.LegacyAccount.ToByteArray(), chunk);
            foreach (var kv in itemResult)
            {
                var item = (ITradableItem)ItemFactory.Deserialize((Dictionary) kv.Value);
                // Avoid Exception when deserialize tradableId
                var _ = item.TradableId;
                itemBag.Add(item);
            }
        });
        return itemBag.ToList();
    }

    public async Task<Dictionary<Address, IValue>> GetStates(byte[] hashBytes, byte[] accountBytes, List<byte[]> addressList)
    {
        var result = new ConcurrentDictionary<Address, IValue>();
        var queryResult = await Service.GetBulkStateByStateRootHash(hashBytes, accountBytes, addressList);
        queryResult
            .AsParallel()
            .WithDegreeOfParallelism(MaxDegreeOfParallelism)
            .ForAll(kv =>
            {
                result.TryAdd(new Address(kv.Key), DeCompressState(kv.Value));
            });
        return result.ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    /// <summary>
    /// GetBulkState with chunking size 1000
    /// </summary>
    /// <param name="hashBytes"></param>
    /// <param name="accountBytes"></param>
    /// <param name="addressList"></param>
    /// <returns></returns>
    public async Task<Dictionary<Address, IValue>> GetChunkedStates(byte[] hashBytes, byte[] accountBytes, List<byte[]> addressList)
    {
        var result = new ConcurrentDictionary<Address, IValue>();
        var chunks = addressList
            .Select((x, i) => new {Index = i, Value = x})
            .GroupBy(x => x.Index / 1000)
            .Select(x => x.Select(v => v.Value).ToList())
            .ToList();
        await Parallel.ForEachAsync(chunks, _parallelOptions, async (chunk, token) =>
        {
            var queryResult = await Service.GetBulkStateByStateRootHash(hashBytes, accountBytes, chunk);
            foreach (var kv in queryResult) result[new Address(kv.Key)] = DeCompressState(kv.Value);
        });

        return result.ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    /// <summary>
    /// Get <see cref="Dictionary{Address,AgentState}"/> for listing avatar addresses.
    /// </summary>
    /// <param name="hashBytes"></param>
    /// <param name="addressList"></param>
    /// <returns></returns>
    public async Task<Dictionary<Address, AgentState>> GetAgentStates(byte[] hashBytes, List<byte[]> addressList)
    {
        var result = new ConcurrentDictionary<Address, AgentState>();
        var queryResult = await Service.GetAgentStatesByStateRootHash(hashBytes, addressList);
        queryResult
            .AsParallel()
            .WithDegreeOfParallelism(MaxDegreeOfParallelism)
            .ForAll(kv =>
            {
                var iValue = DeCompressState(kv.Value);
                if (iValue is Dictionary dict)
                {
                    result.TryAdd(new Address(kv.Key), new AgentState(dict));
                }
                else if (iValue is List list)
                {
                    result.TryAdd(new Address(kv.Key), new AgentState(list));
                }
            });

        return result.ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    /// <summary>
    /// Get <see cref="MarketState"/> for listing avatar addresses.
    /// </summary>
    /// <param name="hashBytes"></param>
    /// <returns><see cref="Task{MarketState}"/></returns>
    public async Task<MarketState> GetMarket(byte[] hashBytes)
    {
        var marketResult = await Service.GetStateByStateRootHash(
            hashBytes,
            ReservedAddresses.LegacyAccount.ToByteArray(),
            Addresses.Market.ToByteArray());
        var value = DeCompressState(marketResult);
        if (value is List list) return new MarketState(list);

        return new MarketState();
    }

    /// <summary>
    /// Get <see cref="List{T}"/> of <see cref="OrderDigest"/> from avatar addresses.
    /// </summary>
    /// <param name="avatarAddresses"></param>
    /// <param name="hashBytes"></param>
    /// <returns><see cref="List{T}"/> of <see cref="OrderDigest"/></returns>
    public async Task<List<OrderDigest>> GetOrderDigests(List<Address> avatarAddresses, byte[] hashBytes)
    {
        var digestListStateAddresses =
            avatarAddresses.Select(a => OrderDigestListState.DeriveAddress(a).ToByteArray()).ToList();
        var digestListStateResult = await GetStates(
            hashBytes,
            ReservedAddresses.LegacyAccount.ToByteArray(),
            digestListStateAddresses);
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

    public async Task DeleteProducts(List<Guid> deletedIds, MarketContext marketContext)
    {
        // 등록취소, 판매된 경우 해당 row를 삭제함
        if (deletedIds.Any())
        {
            await marketContext.Products.Where(p => deletedIds.Contains(p.ProductId)).ExecuteDeleteAsync();
        }
    }

    internal class LocalRandom : Random, IRandom
    {
        public int Seed { get; }

        public LocalRandom(int seed) : base(seed)
        {
            Seed = seed;
        }
    }

    private IValue DeCompressState(byte[] compressed)
    {
        using (var cp = new MemoryStream(compressed))
        {
            using (var decompressed = new MemoryStream())
            {
                using (var df = new DeflateStream(cp, CompressionMode.Decompress))
                {
                    df.CopyTo(decompressed);
                    decompressed.Seek(0, SeekOrigin.Begin);
                    var dec = decompressed.ToArray();
                    return _codec.Decode(dec);
                }
            }
        }
    }
}
