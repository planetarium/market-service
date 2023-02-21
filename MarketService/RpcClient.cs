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
using Nekoyume.Helper;
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
                MaxReceiveMessageSize = null
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

    public async Task SyncOrder(ItemSubType itemSubType, byte[] hashBytes,
        CrystalEquipmentGrindingSheet crystalEquipmentGrindingSheet,
        CrystalMonsterCollectionMultiplierSheet crystalMonsterCollectionMultiplierSheet)
    {
        while (!Init) await Task.Delay(100);

        try
        {
            var sw = new Stopwatch();
            sw.Start();
            var addressList = GetShopAddress(itemSubType);
            var result = await Service.GetStateBulk(addressList, hashBytes);
            sw.Stop();
            var shopStates = GetShopStates(result);
            _logger.LogInformation("Get ShopStateRaw: {Ts}", sw.Elapsed);
            sw.Restart();
            var chainIds = new List<Guid>();
            var orderDigestList = new List<OrderDigest>();
            var tradableIds = new List<Guid>();
            foreach (var shopState in shopStates)
            foreach (var orderDigest in shopState.OrderDigestList)
            {
                chainIds.Add(orderDigest.OrderId);
                orderDigestList.Add(orderDigest);
                if (!tradableIds.Contains(orderDigest.TradableId)) tradableIds.Add(orderDigest.TradableId);
            }

            var marketContext = await _contextFactory.CreateDbContextAsync();
            var existIds = marketContext.Database
                .SqlQueryRaw<Guid>(
                    $"Select productid from products where exist = {true} and legacy = {true} and itemsubtype = {(int) itemSubType}")
                .ToList();
            var deletedIds = existIds.Where(i => !chainIds.Contains(i)).ToList();
            var orderIds = chainIds.Where(i => !existIds.Contains(i)).ToList();
            sw.Stop();
            _logger.LogInformation(
                $"existIds: {existIds.Count}, deletedIds: {deletedIds.Count}, orderIds: {orderIds.Count}");
            _logger.LogInformation("Get TargetIds: {Ts}", sw.Elapsed);
            sw.Restart();
            // var purchasedIds = GetOrderPurchasedIds(deletedIds, hashBytes);
            await InsertOrders(itemSubType, hashBytes, orderIds, tradableIds, marketContext, orderDigestList,
                crystalEquipmentGrindingSheet, crystalMonsterCollectionMultiplierSheet);
            sw.Stop();
            _logger.LogInformation("InsertOrders: {Ts}", sw.Elapsed);
            sw.Restart();
            await UpdateLegacyProducts(deletedIds, chainIds, itemSubType);
            sw.Stop();
            _logger.LogInformation("UpdateProducts: {Ts}", sw.Elapsed);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unexpected exception occurred during RaiderWorker: {Exc}", e);
        }
    }

    private async Task InsertOrders(ItemSubType itemSubType, byte[] hashBytes, List<Guid> orderIds,
        List<Guid> tradableIds,
        MarketContext marketContext, List<OrderDigest> orderDigestList,
        CrystalEquipmentGrindingSheet crystalEquipmentGrindingSheet,
        CrystalMonsterCollectionMultiplierSheet crystalMonsterCollectionMultiplierSheet)
    {
        var sw = new Stopwatch();
        sw.Start();
        var orders = await GetOrders(orderIds, hashBytes);
        sw.Stop();
        _logger.LogInformation("GetOrders: {Ts}", sw.Elapsed);
        sw.Restart();
        var items = await GetItems(tradableIds, hashBytes);
        sw.Stop();
        _logger.LogInformation("GetItems: {Ts}", sw.Elapsed);
        sw.Restart();
        var list = new List<ProductModel>();
        foreach (var order in orders)
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
                Legacy = true
            };
            if (item is ItemUsable itemUsable)
            {
                var map = itemUsable.StatsMap;
                var additionalStats = map.GetAdditionalStats(true).Select(s => new StatModel
                {
                    Additional = true,
                    Type = s.statType,
                    Value = s.additionalValue
                });
                var baseStats = map.GetBaseStats(true).Select(s => new StatModel
                {
                    Additional = false,
                    Type = s.statType,
                    Value = s.baseValue
                });
                var stats = new List<StatModel>();
                stats.AddRange(additionalStats);
                stats.AddRange(baseStats);
                itemProduct.Stats = stats;
            }

            if (item is Equipment equipment)
            {
                itemProduct.ElementalType = equipment.ElementalType;
                itemProduct.SetId = equipment.SetId;
                itemProduct.CombatPoint = orderDigest.CombatPoint;
                itemProduct.Level = equipment.level;
                itemProduct.Grade = equipment.Grade;
                var skillModels = new List<SkillModel>();
                skillModels.AddRange(equipment.Skills.Select(s => new SkillModel
                {
                    SkillId = s.SkillRow.Id,
                    Power = s.Power,
                    Chance = s.Chance,
                    ElementalType = s.SkillRow.ElementalType,
                    SkillCategory = s.SkillRow.SkillCategory,
                    HitCount = s.SkillRow.HitCount,
                    Cooldown = s.SkillRow.Cooldown
                }));
                skillModels.AddRange(equipment.BuffSkills.Select(s => new SkillModel
                {
                    SkillId = s.SkillRow.Id,
                    Power = s.Power,
                    Chance = s.Chance,
                    ElementalType = s.SkillRow.ElementalType,
                    SkillCategory = s.SkillRow.SkillCategory,
                    HitCount = s.SkillRow.HitCount,
                    Cooldown = s.SkillRow.Cooldown
                }));
                itemProduct.Skills = skillModels;
                var crystal = CrystalCalculator.CalculateCrystal(
                    new[] {equipment},
                    false,
                    crystalEquipmentGrindingSheet,
                    crystalMonsterCollectionMultiplierSheet,
                    0);
                itemProduct.Crystal = (int) crystal.MajorUnit;
                itemProduct.CrystalPerPrice = (int) crystal
                    .DivRem(orderDigest.Price.MajorUnit).Quotient.MajorUnit;
            }

            list.Add(itemProduct);
        }

        foreach (var chunk in list.Chunk(1000))
        {
            await marketContext.Products.AddRangeAsync(chunk);
            await marketContext.SaveChangesAsync();
        }

        sw.Stop();
        _logger.LogInformation("Insert rows: {Ts}", sw.Elapsed);
        _logger.LogInformation($"{itemSubType}: {list.Count}");
    }

    public async Task SyncProduct(byte[] hashBytes)
    {
        while (!Init) await Task.Delay(100);

        try
        {
            var costumeStatSheet = await GetCostumeStatSheet(hashBytes);
            var marketState = await GetMarket(hashBytes);
            var avatarAddressList = marketState.AvatarAddresses;
            var deletedIds = new List<Guid>();
            var chainIds = new List<Guid>();
            var products = new List<Product>();
            var productStates = await GetProductStates(avatarAddressList, hashBytes);
            foreach (var kv in productStates)
            {
                chainIds.Add(kv.Key);
                if (kv.Value.Equals(Null.Value)) deletedIds.Add(kv.Key);

                if (kv.Value is List deserialized) products.Add(ProductFactory.Deserialize(deserialized));
            }

            await InsertProducts(products, costumeStatSheet);
            await UpdateProducts(deletedIds, chainIds);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unexpected exception occurred during RaiderWorker: {Exc}", e);
        }
    }

    private async Task UpdateProducts(List<Guid> targetIds, List<Guid> chainProductIds)
    {
        // 등록취소, 판매된 경우 Exist 필드를 업데이트함. 
        // Product의 상태가 Null이거나 DB에는 남아 있으나 체인상 ProductList에 해당 아이디가 없는 경우
        var marketContext = await _contextFactory.CreateDbContextAsync();
        if (targetIds.Any())
        {
            var param = new NpgsqlParameter("@targetIds", targetIds);
            await marketContext.Database.ExecuteSqlRawAsync(
                $"UPDATE products set exist = {false} WHERE productid = any(@targetIds)", param);
        }

        if (chainProductIds.Any())
        {
            var param = new NpgsqlParameter("@chainIds", chainProductIds);
            await marketContext.Database.ExecuteSqlRawAsync(
                $"UPDATE products set exist = {false} WHERE not productid = any(@chainIds)", param);
        }

        _logger.LogInformation($"UpdateProducts: {targetIds.Count}");
    }

    private async Task UpdateLegacyProducts(List<Guid> targetIds, List<Guid> chainProductIds, ItemSubType itemSubType)
    {
        // 등록취소, 판매된 경우 Exist 필드를 업데이트함. 
        // Product의 상태가 Null이거나 DB에는 남아 있으나 체인상 ProductList에 해당 아이디가 없는 경우
        var marketContext = await _contextFactory.CreateDbContextAsync();
        if (targetIds.Any())
        {
            var param = new NpgsqlParameter("@targetIds", targetIds);
            await marketContext.Database.ExecuteSqlRawAsync(
                $"UPDATE products set exist = {false} WHERE itemsubtype = {(int) itemSubType} and legacy = {true} and productid = any(@targetIds)",
                param);
        }

        if (chainProductIds.Any())
        {
            var param = new NpgsqlParameter("@chainIds", chainProductIds);
            await marketContext.Database.ExecuteSqlRawAsync(
                $"UPDATE products set exist = {false} WHERE itemsubtype = {(int) itemSubType} and legacy = {true} and not productid = any(@chainIds)",
                param);
        }

        _logger.LogInformation($"UpdateProducts: {targetIds.Count}");
    }

    private async Task InsertProducts(List<Product> products, CostumeStatSheet costumeStatSheet)
    {
        var marketContext = await _contextFactory.CreateDbContextAsync();
        var existProductIds = marketContext.Products.AsNoTracking().Select(p => p.ProductId);
        var filteredProducts = products.Where(p => !existProductIds.Contains(p.ProductId));
        var itemProducts = filteredProducts.OfType<ItemProduct>().ToList();
        var list = new List<ProductModel>();
        var items = itemProducts.Select(i => i.TradableItem).ToList();
        foreach (var itemProduct in itemProducts)
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
#pragma warning disable CS0618
                CombatPoint = CPHelper.GetCP(itemProduct.TradableItem, costumeStatSheet),
#pragma warning restore CS0618
                Exist = true
            };
            if (item is ItemUsable itemUsable)
            {
                var map = itemUsable.StatsMap;
                var additionalStats = map.GetAdditionalStats(true).Select(s => new StatModel
                {
                    Additional = true,
                    Type = s.statType,
                    Value = s.additionalValue
                });
                var baseStats = map.GetBaseStats(true).Select(s => new StatModel
                {
                    Additional = false,
                    Type = s.statType,
                    Value = s.baseValue
                });
                var stats = new List<StatModel>();
                stats.AddRange(additionalStats);
                stats.AddRange(baseStats);
                itemProductModel.Stats = stats;
            }

            if (item is Equipment equipment)
            {
                itemProductModel.ElementalType = equipment.ElementalType;
                itemProductModel.SetId = equipment.SetId;
                itemProductModel.Level = equipment.level;
                itemProductModel.Grade = equipment.Grade;
                var skillModels = new List<SkillModel>();
                skillModels.AddRange(equipment.Skills.Select(s => new SkillModel
                {
                    SkillId = s.SkillRow.Id,
                    Power = s.Power,
                    Chance = s.Chance,
                    ElementalType = s.SkillRow.ElementalType,
                    SkillCategory = s.SkillRow.SkillCategory,
                    HitCount = s.SkillRow.HitCount,
                    Cooldown = s.SkillRow.Cooldown
                }));
                skillModels.AddRange(equipment.BuffSkills.Select(s => new SkillModel
                {
                    SkillId = s.SkillRow.Id,
                    Power = s.Power,
                    Chance = s.Chance,
                    ElementalType = s.SkillRow.ElementalType,
                    SkillCategory = s.SkillRow.SkillCategory,
                    HitCount = s.SkillRow.HitCount,
                    Cooldown = s.SkillRow.Cooldown
                }));
                itemProductModel.Skills = skillModels;
            }

            list.Add(itemProductModel);
        }

        await marketContext.Products.AddRangeAsync(list);
        await marketContext.SaveChangesAsync();
    }

    public async Task<CostumeStatSheet> GetCostumeStatSheet(byte[] hashBytes)
    {
        var sheetAddress = Addresses.GetSheetAddress<CostumeStatSheet>();
        var result = await Service.GetState(sheetAddress.ToByteArray(), hashBytes);
        if (_codec.Decode(result) is Text t)
        {
            var sheet = new CostumeStatSheet();
            sheet.Set(t);
            return sheet;
        }

        throw new Exception();
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

    private async Task<Dictionary<Guid, IValue>> GetProductStates(IEnumerable<Address> avatarAddressList,
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

    private List<ProductsState> GetProductsState(Dictionary<Address, IValue> queryResult)
    {
        var result = new List<ProductsState>();
        foreach (var kv in queryResult)
            if (kv.Value is List list)
                result.Add(new ProductsState(list));

        return result;
    }

    private IEnumerable<byte[]> GetShopAddress(ItemSubType itemSubType)
    {
        if (ShardedSubTypes.Contains(itemSubType))
        {
            var addressList = ShardedShopState.AddressKeys.Select(nonce =>
                ShardedShopStateV2.DeriveAddress(itemSubType, nonce).ToByteArray());
            return addressList;
        }

        return new[] {ShardedShopStateV2.DeriveAddress(itemSubType, "").ToByteArray()};
    }

    private IEnumerable<ShardedShopStateV2> GetShopStates(Dictionary<byte[], byte[]> queryResult)
    {
        var result = new List<ShardedShopStateV2>();
        foreach (var kv in queryResult)
        {
            var decode = _codec.Decode(kv.Value);
            if (decode is Dictionary dictionary) result.Add(new ShardedShopStateV2(dictionary));
        }

        return result;
    }

    private async Task<List<Order>> GetOrders(IEnumerable<Guid> orderIds, byte[] hashBytes)
    {
        var orderAddressList = orderIds.Select(i => Order.DeriveAddress(i).ToByteArray()).ToList();
        var orderResult = await GetChunkedStates(orderAddressList, hashBytes);
        return orderResult.Select(kv => OrderFactory.Deserialize((Dictionary) kv.Value)).ToList();
    }

    private async Task<List<ItemBase>> GetItems(IEnumerable<Guid> tradableIds, byte[] hashBytes)
    {
        var itemAddressList = tradableIds.Select(i => Addresses.GetItemAddress(i).ToByteArray()).ToList();
        var itemResult = await GetChunkedStates(itemAddressList, hashBytes);

        return itemResult.Select(kv => ItemFactory.Deserialize((Dictionary) kv.Value)).ToList();
    }

    private async Task<List<Guid>> GetOrderPurchasedIds(IEnumerable<Guid> orderIds, byte[] hashBytes)
    {
        var receiptAddressList = orderIds.Select(i => OrderReceipt.DeriveAddress(i).ToByteArray()).ToList();
        var receiptResult = await GetChunkedStates(receiptAddressList, hashBytes);
        var result = new List<Guid>();
        foreach (var kv in receiptResult)
            if (kv.Value is Dictionary dictionary)
            {
                var receipt = new OrderReceipt(dictionary);
                result.Add(receipt.OrderId);
            }

        return result;
    }

    private async Task<Dictionary<Address, IValue>> GetChunkedStates(List<byte[]> addressList, byte[] hashBytes)
    {
        var result = new Dictionary<Address, IValue>();
        var chunks = addressList
            .Select((x, i) => new {Index = i, Value = x})
            .GroupBy(x => x.Index / 1000)
            .Select(x => x.Select(v => v.Value).ToList())
            .ToList();
        var sw = new Stopwatch();
        sw.Start();
        for (var i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            var queryResult = await Service.GetStateBulk(chunk, hashBytes);
            sw.Stop();
            _logger.LogInformation($"GetChunked States {i} / {chunks.Count}: {sw.Elapsed}");
            sw.Restart();
            foreach (var kv in queryResult) result[new Address(kv.Key)] = _codec.Decode(kv.Value);
            sw.Stop();
            _logger.LogInformation($"Deserialized result {i} / {chunks.Count}: {sw.Elapsed}");
            sw.Restart();
        }

        sw.Stop();
        // foreach (var chunked in addressList.Chunk(500))
        // {
        //     var queryResult = await Service.GetStateBulk(chunked, hashBytes);
        //
        //     await Task.Delay(100);
        // }

        return result;
    }

    private async Task<MarketState> GetMarket(byte[] hashBytes)
    {
        var marketResult = await Service.GetState(Addresses.Market.ToByteArray(), hashBytes);
        var value = _codec.Decode(marketResult);
        if (value is List list) return new MarketState(list);

        return new MarketState();
    }
}
