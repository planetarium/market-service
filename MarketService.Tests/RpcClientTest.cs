using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bencodex;
using Bencodex.Types;
using Grpc.Core;
using Lib9c.Model.Order;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Libplanet.Mocks;
using Libplanet.Types.Blocks;
using MagicOnion;
using MagicOnion.Server;
using MarketService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nekoyume;
using Nekoyume.Model.Item;
using Nekoyume.Model.Market;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.Shared.Services;
using Nekoyume.TableData;
using Nekoyume.TableData.Crystal;
using Xunit;
using Xunit.Abstractions;

namespace MarketService.Tests;

public class RpcClientTest
{
    private readonly CrystalEquipmentGrindingSheet _crystalEquipmentGrindingSheet;
    private readonly CrystalMonsterCollectionMultiplierSheet _crystalMonsterCollectionMultiplierSheet;
    private readonly CostumeItemSheet _costumeItemSheet;
    private readonly CostumeStatSheet _costumeStatSheet;
    private readonly EquipmentItemSheet.Row _row;
    private readonly TestService _testService;
    private readonly string _connectionString;
    private readonly Currency _currency;
    private readonly RpcClient _client;
    private readonly DbContextFactory<MarketContext> _contextFactory;
    private readonly ITestOutputHelper _output;

    public RpcClientTest(ITestOutputHelper output)
    {
        _output = output;
        _testService = new TestService();
        _row = new EquipmentItemSheet.Row();
        _row.Set(@"10200000,Armor,1,Normal,0,HP,30,2,Character/Player/10200000".Split(","));
        _crystalEquipmentGrindingSheet = new CrystalEquipmentGrindingSheet();
        _crystalEquipmentGrindingSheet.Set(@"id,enchant_base_id,gain_crystal
10100000,10100000,10
10110000,10110000,10
10111000,10110000,19
10112000,10110000,19
10113000,10110000,184
10114000,10110000,636
10120000,10120000,80
10121000,10120000,101
10122000,10120000,144
10123000,10120000,1056
10124000,10120000,11280
10130000,10130000,2340
10131000,10130000,2784
10132000,10130000,3168
10133000,10130000,20520
10134000,10130000,55680
10130001,10130001,8340
10131001,10130001,14520
10132001,10130001,16020
10133001,10130001,72060
10134001,10130001,165300
10140000,10141000,2000000
10141000,10141000,1000000
10142000,10141000,1000000
10143000,10141000,1500000
10144000,10141000,1500000
10140001,10140001,306600
10141001,10140001,329640
10142001,10140001,332820
10143001,10140001,332820
10144001,10140001,337380
10150000,10150000,1008600
10151000,10150000,1046220
10152000,10150000,1056780
10153000,10150000,1065960
10154000,10150000,1076520
10150001,10150001,1123800
10151001,10150001,1164660
10152001,10150001,1175220
10153001,10150001,1194960
10154001,10150001,1204140
10155000,10155000,1200600
10200000,10200000,10
10210000,10210000,10
10211000,10210000,19
10212000,10210000,27
10213000,10210000,230
10214000,10210000,742
10220000,10220000,50
10221000,10220000,74
10222000,10220000,103
10223000,10220000,928
10224000,10220000,11960
10230000,10230000,3000
10231000,10230000,3480
10232000,10230000,3864
10233000,10230000,46800
10234000,10230000,60840
10230001,10230001,9600
10231001,10230001,16500
10232001,10230001,17520
10233001,10230001,151800
10234001,10230001,296400
10240000,10241000,2000000
10241000,10241000,1000000
10242000,10241000,1000000
10243000,10241000,1500000
10244000,10241000,1500000
10240001,10240001,260760
10241001,10240001,282900
10242001,10240001,286620
10243001,10240001,286620
10244001,10240001,289800
10250000,10250000,807840
10251000,10250000,831600
10252000,10250000,841320
10253000,10250000,850500
10254000,10250000,888300
10250001,10250001,1019160
10251001,10250001,1058400
10252001,10250001,1068120
10253001,10250001,1125180
10254001,10250001,1135740
10255000,10255000,1129320
10310000,10310000,10
10311000,10310000,19
10312000,10310000,41
10313000,10310000,252
10314000,10310000,954
10320000,10320000,120
10321000,10320000,144
10322000,10320000,216
10323000,10320000,1496
10324000,10320000,40740
10330000,10330000,3300
10331000,10330000,3864
10332000,10330000,4872
10333000,10330000,67860
10334000,10330000,247420
10340000,10340000,24948
10341000,10340000,28980
10342000,10340000,376200
10343000,10340000,376200
10344000,10340000,376200
10350000,10351000,2000000
10351000,10351000,1000000
10352000,10351000,1000000
10353000,10351000,1500000
10354000,10351000,1500000
10410000,10410000,10
10411000,10410000,19
10412000,10410000,41
10413000,10410000,315
10414000,10410000,1060
10420000,10420000,140
10421000,10420000,193
10422000,10420000,245
10423000,10420000,1736
10424000,10420000,38960
10430000,10430000,4320
10431000,10430000,4560
10432000,10430000,5952
10433000,10430000,65040
10434000,10430000,157500
10440000,10440000,243000
10441000,10440000,509400
10442000,10440000,509400
10443000,10440000,509400
10444000,10440000,509400
10450000,10451000,2000000
10451000,10451000,1000000
10452000,10451000,1000000
10453000,10451000,1500000
10454000,10451000,1500000
10510000,10510000,10
10511000,10510000,19
10512000,10510000,46
10513000,10510000,373
10514000,10510000,1272
10520000,10520000,160
10521000,10520000,216
10522000,10520000,288
10523000,10520000,1904
10524000,10520000,44300
10530000,10530000,4980
10531000,10530000,5568
10532000,10530000,6648
10533000,10530000,249600
10534000,10530000,296400
10540000,10541000,2000000
10541000,10541000,1000000
10542000,10541000,1000000
10543000,10541000,1500000
10544000,10541000,1500000
10550000,10550000,918000
10551000,10550000,954720
10552000,10550000,963900
10553000,10550000,973620
10554000,10550000,1026480
11320000,10320000,120
11420000,10420000,140
11520000,10520000,160");
        _crystalMonsterCollectionMultiplierSheet = new CrystalMonsterCollectionMultiplierSheet();
        _crystalMonsterCollectionMultiplierSheet.Set(@"level,multiplier
0,0
1,0
2,50
3,100
4,200
5,300");

        var host = Environment.GetEnvironmentVariable("TEST_DB_HOST") ?? "localhost";
        var userName = Environment.GetEnvironmentVariable("TEST_DB_USER") ?? "postgres";
        var pw = Environment.GetEnvironmentVariable("TEST_DB_PW");
        var connectionString = $"Host={host};Username={userName};Database={GetType().Name};";
        if (!string.IsNullOrEmpty(pw))
        {
            connectionString += $"Password={pw};";
        }

        _connectionString = connectionString;
        _costumeStatSheet = new CostumeStatSheet();
        _costumeStatSheet.Set(@"id,costume_id,stat_type,stat
1,40100000,ATK,30
2,40100001,DEF,15
3,40100002,HIT,135
4,40100003,ATK,60
5,40100003,HIT,135
6,40100005,ATK,35
7,40100006,ATK,55
8,40100006,SPD,100
9,49900001,HP,300
10,49900002,HP,450
11,49900003,HP,450
12,49900003,DEF,20
13,49900004,ATK,40
14,49900005,ATK,80
15,49900005,HIT,150
16,49900006,HIT,120
17,40100007,DEF,10
18,40100007,HIT,150
19,40100009,SPD,80
20,40100009,ATK,65
21,40100008,ATK,35
22,49900007,HP,5000
23,49900008,DEF,320
24,40100010,ATK,530
25,49900009,HIT,1210");
        _costumeItemSheet = new CostumeItemSheet();
        _costumeItemSheet.Set(@"id,_name,item_sub_type,grade,elemental_type,spine_resource_path
40100000,발키리,FullCostume,4,Normal,
40100001,새벽의 사자,FullCostume,3,Normal,
40100002,헬라의 환영,FullCostume,5,Normal,
40100003,어둠의 발키리,FullCostume,5,Normal,
40100004,천상의 고양이,FullCostume,2,Normal,
40100005,창술사 루이,FullCostume,4,Normal,
40100006,창술사 루시,FullCostume,5,Normal,
40100007,백마법사 아라엘,FullCostume,5,Normal,
40100008,마법사 릴리,FullCostume,4,Normal,
40100009,흑마법사 이블리,FullCostume,5,Normal,
40100010,도끼 여전사 퓨리오사,FullCostume,5,Normal,
40200001,갈색 머리카락,HairCostume,1,Normal,
40200002,파란색 머리카락,HairCostume,1,Normal,
40200003,초록색 머리카락,HairCostume,1,Normal,
40200004,빨간색 머리카락,HairCostume,1,Normal,
40200005,흰색 머리카락,HairCostume,1,Normal,
40200006,노란색 머리카락,HairCostume,1,Normal,
40200007,검은색 머리카락,HairCostume,1,Normal,
40300001,갈색 귀,EarCostume,1,Normal,
40300002,검은색 귀,EarCostume,1,Normal,
40300003,갈색 표범 무늬 귀,EarCostume,1,Normal,
40300004,회색 표범 무늬 귀,EarCostume,1,Normal,
40300005,흰색 귀,EarCostume,1,Normal,
40300006,검붉은색 귀,EarCostume,1,Normal,
40300007,흰털 갈색 귀,EarCostume,1,Normal,
40300008,흰털 파란색 귀,EarCostume,1,Normal,
40300009,흰털 회색 귀,EarCostume,1,Normal,
40300010,흰털 파란 유령 귀,EarCostume,1,Normal,
40400001,빨간색 눈,EyeCostume,1,Normal,
40400002,파란색 눈,EyeCostume,1,Normal,
40400003,초록색 눈,EyeCostume,1,Normal,
40400004,보라색 눈,EyeCostume,1,Normal,
40400005,흰색 눈,EyeCostume,1,Normal,
40400006,노란색 눈,EyeCostume,1,Normal,
40500001,꼬리,TailCostume,1,Normal,
40500002,검흰색 꼬리,TailCostume,1,Normal,
40500003,갈색 표범 무늬 꼬리,TailCostume,1,Normal,
40500004,회색 표범 무늬 꼬리,TailCostume,1,Normal,
40500005,흰색 꼬리,TailCostume,1,Normal,
40500006,검은색 꼬리,TailCostume,1,Normal,
40500007,빨간색 꼬리,TailCostume,1,Normal,
40500008,파란색 꼬리,TailCostume,1,Normal,
40500009,회색 꼬리,TailCostume,1,Normal,
40500010,파란 유령 꼬리,TailCostume,1,Normal,
49900001,전설의 모험가,Title,4,Normal,
49900002,최초의 전사,Title,5,Normal,
49900003,Yggdrasil Champion,Title,5,Normal,
49900004,Yggdrasil Challenger,Title,4,Normal,
49900005,Sushi Frontier,Title,5,Normal,
49900006,Yggdrasil Warrior,Title,3,Normal,
49900007,Championship 1 Ranker,Title,5,Normal,
49900008,Championship 2 Ranker,Title,5,Normal,
49900009,2022 Grand Finale,Title,3,Normal,");
#pragma warning disable CS0618
        _currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
#pragma warning disable EF1001
        _contextFactory = new DbContextFactory<MarketContext>(null!,
            new DbContextOptionsBuilder<MarketContext>().UseNpgsql(_connectionString)
                .UseLowerCaseNamingConvention().Options, new DbContextFactorySource<MarketContext>());
#pragma warning restore EF1001
        var rpcConfigOptions = new RpcConfigOptions { Host = "localhost", Port = 5000 };
        var receiver = new Receiver(new Logger<Receiver>(new LoggerFactory()));
        using var logger = _output.BuildLoggerFor<RpcClient>();
        _client = new TestClient(new OptionsWrapper<RpcConfigOptions>(rpcConfigOptions),
            logger, receiver, _contextFactory, _testService);
    }

    [Theory]
    [InlineData(ItemSubType.Armor)]
    [InlineData(ItemSubType.FullCostume)]
    public async Task SyncOrder_Cancel(ItemSubType itemSubType)
    {
        var ct = new CancellationToken();
#pragma warning disable EF1001
        var context = await _contextFactory.CreateDbContextAsync(ct);
#pragma warning restore EF1001
        await context.Database.EnsureDeletedAsync(ct);
        await context.Database.EnsureCreatedAsync(ct);
        var agentAddress = new PrivateKey().Address;
        var avatarAddress = new PrivateKey().Address;
        var order = OrderFactory.Create(agentAddress, avatarAddress, Guid.NewGuid(), 1 * _currency, Guid.NewGuid(), 0L,
            itemSubType, 1);
        _testService.SetOrder(order);
        ITradableItem tradableItem = null!;
        ItemBase item = null!;
        if (itemSubType == ItemSubType.Armor)
        {
            tradableItem = (ITradableItem)ItemFactory.CreateItemUsable(_row, order.TradableId, 0L);
        }

        if (itemSubType == ItemSubType.FullCostume)
        {
            var costumeId = 40100007;
            var row = _costumeItemSheet.Values.First(r => r.Id == costumeId);
            tradableItem = ItemFactory.CreateCostume(row, order.TradableId);
        }

        item = (ItemBase)tradableItem;
        var blockIndex = ActionObsoleteConfig.V100080ObsoleteIndex + 1L;

        var orderDigest = new OrderDigest(
            agentAddress,
            blockIndex,
            blockIndex + Order.ExpirationInterval,
            order.OrderId,
            order.TradableId,
            order.Price,
            0,
            0,
            item.Id,
            1
        );
        _testService.SetState(Addresses.GetItemAddress(tradableItem.TradableId), tradableItem.Serialize());
        SetShopStates(itemSubType, orderDigest);
        var agentState = new AgentState(agentAddress);
        agentState.avatarAddresses.Add(0, avatarAddress);
        _testService.SetAgentState(agentAddress, agentState);
        var orderDigestListState = new OrderDigestListState(OrderDigestListState.DeriveAddress(avatarAddress));
        orderDigestListState.Add(orderDigest);
        _testService.SetState(orderDigestListState.Address, orderDigestListState.Serialize());

        // Insert order
        await _client.SyncOrder(null!, _crystalEquipmentGrindingSheet,
            _crystalMonsterCollectionMultiplierSheet, _costumeStatSheet);
        var productModel = Assert.Single(context.ItemProducts);
        Assert.True(productModel.Legacy);
        Assert.True(productModel.Exist);
        Assert.True(productModel.Stats.Any());
        Assert.True(productModel.CombatPoint > 0);
        Assert.Equal(item.Grade, productModel.Grade);
        Assert.Equal(item.ElementalType, productModel.ElementalType);

        // Cancel order
        orderDigestListState.Remove(orderDigest.OrderId);
        _testService.SetState(orderDigestListState.Address, orderDigestListState.Serialize());
        await _client.SyncOrder(null!, _crystalEquipmentGrindingSheet,
            _crystalMonsterCollectionMultiplierSheet, _costumeStatSheet);
#pragma warning disable EF1001
        var nextContext = await _contextFactory.CreateDbContextAsync(ct);
#pragma warning restore EF1001
        var updatedProductModel = Assert.Single(nextContext.Products);
        Assert.Equal(productModel.ProductId, updatedProductModel.ProductId);
        Assert.False(updatedProductModel.Exist);
    }

    [Theory]
    [InlineData(ItemSubType.Armor)]
    public async Task SyncOrder_ReRegister(ItemSubType itemSubType)
    {
        var ct = new CancellationToken();
#pragma warning disable EF1001
        var context = await _contextFactory.CreateDbContextAsync(ct);
#pragma warning restore EF1001
        await context.Database.EnsureDeletedAsync(ct);
        await context.Database.EnsureCreatedAsync(ct);
        var agentAddress = new PrivateKey().Address;
        var avatarAddress = new PrivateKey().Address;
        var order = OrderFactory.Create(agentAddress, avatarAddress, Guid.NewGuid(), 1 * _currency, Guid.NewGuid(), 0L,
            itemSubType, 1);
        _testService.SetOrder(order);
        var item = ItemFactory.CreateItemUsable(_row, order.TradableId, 0L);
        ((Equipment) item).optionCountFromCombination = 1;
        var blockIndex = ActionObsoleteConfig.V100080ObsoleteIndex + 1L;
        var orderDigest = new OrderDigest(
            agentAddress,
            blockIndex,
            blockIndex + Order.ExpirationInterval,
            order.OrderId,
            order.TradableId,
            order.Price,
            0,
            0,
            item.Id,
            1
        );
        SetShopStates(itemSubType, orderDigest);
        _testService.SetState(Addresses.GetItemAddress(item.NonFungibleId), item.Serialize());

        var agentState = new AgentState(agentAddress);
        agentState.avatarAddresses.Add(0, avatarAddress);
        _testService.SetAgentState(agentAddress, agentState);
        var orderDigestListState = new OrderDigestListState(OrderDigestListState.DeriveAddress(avatarAddress));
        orderDigestListState.Add(orderDigest);
        _testService.SetState(orderDigestListState.Address, orderDigestListState.Serialize());

        // Insert order
        await _client.SyncOrder(null!, _crystalEquipmentGrindingSheet,
            _crystalMonsterCollectionMultiplierSheet, _costumeStatSheet);
        var productModel = Assert.Single(context.ItemProducts);
        Assert.True(productModel.Legacy);
        Assert.True(productModel.Exist);
        Assert.Equal(1, productModel.OptionCountFromCombination);

        // ReRegister order
        var order2 = OrderFactory.Create(agentAddress, avatarAddress, Guid.NewGuid(), 2 * _currency, order.TradableId,
            1L,
            itemSubType, 1);
        var orderDigest2 = new OrderDigest(
            agentAddress,
            blockIndex + 1L,
            blockIndex + Order.ExpirationInterval + 1L,
            order2.OrderId,
            order2.TradableId,
            order2.Price,
            0,
            0,
            item.Id,
            1
        );
        orderDigestListState.Remove(orderDigest.OrderId);
        orderDigestListState.Add(orderDigest2);
        _testService.SetState(Order.DeriveAddress(order2.OrderId), order2.Serialize());
        _testService.SetState(orderDigestListState.Address, orderDigestListState.Serialize());

        await _client.SyncOrder(null!, _crystalEquipmentGrindingSheet,
            _crystalMonsterCollectionMultiplierSheet, _costumeStatSheet);
#pragma warning disable EF1001
        var nextContext = await _contextFactory.CreateDbContextAsync(ct);
#pragma warning restore EF1001
        Assert.Equal(2, nextContext.Products.Count());
        var oldProduct = nextContext.Products.Single(p => p.ProductId == order.OrderId);
        Assert.Equal(1, oldProduct.Price);
        Assert.False(oldProduct.Exist);
        var newProduct = nextContext.Products.Single(p => p.ProductId == order2.OrderId);
        Assert.Equal(2, newProduct.Price);
        Assert.True(newProduct.Exist);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(10510000)]
    public async Task SyncProduct(int? iconId)
    {
        var ct = new CancellationToken();
#pragma warning disable EF1001
        var context = await _contextFactory.CreateDbContextAsync(ct);
#pragma warning restore EF1001
        await context.Database.EnsureDeletedAsync(ct);
        await context.Database.EnsureCreatedAsync(ct);
        var marketState = new MarketState();
        var productsStates = new Dictionary<Address, ProductsState>();
        for (int i = 0; i < 10; i++)
        {
            var agentAddress = new PrivateKey().Address;
            var avatarAddress = new PrivateKey().Address;
            var productState = new ProductsState();
            marketState.AvatarAddresses.Add(avatarAddress);
            for (int j = 0; j < 10; j++)
            {
                var tradableId = Guid.NewGuid();
                var productId = Guid.NewGuid();
                var item = (Equipment)ItemFactory.CreateItemUsable(_row, tradableId, 1L, i + 1);
                item.IconId = (int)iconId!;

                var itemProduct = new ItemProduct
                {
                    ProductId = productId,
                    Price = (j + 1) * _currency,
                    ItemCount = 1,
                    RegisteredBlockIndex = 1L,
                    Type = ProductType.NonFungible,
                    SellerAgentAddress = agentAddress,
                    SellerAvatarAddress = avatarAddress,
                    TradableItem = (ITradableItem)item
                };
                productState.ProductIds.Add(productId);
                _testService.SetState(Product.DeriveAddress(productId), itemProduct.Serialize());
            }

            var productsStateAddress = ProductsState.DeriveAddress(avatarAddress);
            _testService.SetState(productsStateAddress, productState.Serialize());
            productsStates[productsStateAddress] = productState;
        }

        _testService.SetState(Addresses.Market, marketState.Serialize());
        await _client.SyncProduct(null!, _crystalEquipmentGrindingSheet, _crystalMonsterCollectionMultiplierSheet,
            _costumeStatSheet);
#pragma warning disable EF1001
#pragma warning restore EF1001
        var products = context.Products.AsNoTracking().ToList();
        Assert.Equal(100, products.Count);
        foreach (var product in products)
        {
            Assert.True(product.Exist);
            if (product is ItemProductModel itemProduct)
            {
                Assert.True(itemProduct.CombatPoint > 0);
                Assert.True(itemProduct.Level > 0);
                Assert.Equal(1, itemProduct.Grade);
                // If item does not have IconId, IconId is same as ItemId
                Assert.Equal(iconId ?? _row.Id, itemProduct.IconId);
            }
        }

        // Cancel or Buy(deleted from chains)
        foreach (var (key, productsState) in productsStates)
        {
            productsState.ProductIds = productsState.ProductIds.Skip(1).ToList();
            _testService.SetState(key, productsState.Serialize());
        }

        await _client.SyncProduct(null!, _crystalEquipmentGrindingSheet, _crystalMonsterCollectionMultiplierSheet,
            _costumeStatSheet);
#pragma warning disable EF1001
        var nextContext = await _contextFactory.CreateDbContextAsync(ct);
#pragma warning restore EF1001
        var nextProducts = nextContext.Products.AsNoTracking().ToList();
        Assert.Equal(90, nextProducts.Count(p => p.Exist));
        Assert.Equal(10, nextProducts.Count(p => !p.Exist));
    }

    [Fact]
    public async Task GetOrderDigests()
    {
        var ct = new CancellationToken();
#pragma warning disable EF1001
        var context = await _contextFactory.CreateDbContextAsync(ct);
#pragma warning restore EF1001
        await context.Database.EnsureDeletedAsync(ct);
        await context.Database.EnsureCreatedAsync(ct);

        var shopAddresses = _client.GetShopAddress(ItemSubType.Armor).Select(a => new Address(a)).ToArray();

        foreach (var shopAddress in shopAddresses)
        {
            var shopState = new ShardedShopStateV2(shopAddress);
            var agentAddress = new PrivateKey().Address;
            var orderDigest = new OrderDigest(agentAddress, 0L, 1L, Guid.NewGuid(), Guid.NewGuid(), 1 * _currency, 1, 1,
                1, 1);
            shopState.Add(orderDigest, 0L);
            _testService.SetState(shopAddress, shopState.Serialize());
        }

        var orderDigests = await _client.GetOrderDigests(ItemSubType.Armor, null!);

        Assert.Equal(shopAddresses.Length, orderDigests.Count);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UpdateProducts(bool legacy)
    {
        var ct = new CancellationToken();
#pragma warning disable EF1001
        var context = await _contextFactory.CreateDbContextAsync(ct);
#pragma warning restore EF1001
        await context.Database.EnsureDeletedAsync(ct);
        await context.Database.EnsureCreatedAsync(ct);
        var productPrice = 2 * _currency;
        for (int i = 0; i < 2; i++)
        {
            ProductModel itemProduct = new ItemProductModel
            {
                ProductId = Guid.NewGuid(),
                SellerAgentAddress = new PrivateKey().Address,
                Quantity = 1,
                Price = decimal.Parse(productPrice.GetQuantityString()),
                SellerAvatarAddress = new PrivateKey().Address,
                ItemId = 3,
                Exist = true,
                ItemSubType = ItemSubType.Armor,
                Legacy = legacy
            };
            ProductModel favProduct = new FungibleAssetValueProductModel
            {
                ProductId = Guid.NewGuid(),
                SellerAgentAddress = new PrivateKey().Address,
                Quantity = 2,
                Price = decimal.Parse(productPrice.GetQuantityString()),
                SellerAvatarAddress = new PrivateKey().Address,
                Exist = true,
                Legacy = !legacy
            };
            await context.Products.AddRangeAsync(itemProduct, favProduct);
        }

        await context.SaveChangesAsync(ct);

        var products = context.Products.AsNoTracking().ToList();
        Assert.All(products, product => Assert.True(product.Exist));
        Assert.Equal(4, products.Count);

        var deletedId = products.First(p => p.Legacy == legacy).ProductId;
        var deletedIds = new List<Guid>
        {
            deletedId,
            products.First(p => p.Legacy != legacy).ProductId
        };

        await _client.UpdateProducts(deletedIds, context, legacy);

#pragma warning disable EF1001
        var nextContext = await _contextFactory.CreateDbContextAsync(ct);
#pragma warning restore EF1001
        var nextProducts = nextContext.Products.AsNoTracking().ToList();
        Assert.Equal(4, nextProducts.Count);
        var existProducts = nextProducts.Where(p => p.Exist).ToList();
        Assert.Equal(3, existProducts.Count);
        var existProduct = Assert.Single(existProducts.Where(p => p.Exist && p.Legacy == legacy));
        Assert.NotEqual(deletedId, existProduct.ProductId);
    }

    private class TestClient : RpcClient
    {
        public TestClient(IOptions<RpcConfigOptions> options, ILogger<RpcClient> logger, Receiver receiver,
            IDbContextFactory<MarketContext> contextFactory, TestService service) : base(options, logger, receiver,
            contextFactory)
        {
            Service = service;
            var path = "../../../genesis";
            var buffer = File.ReadAllBytes(path);
            var dict = (Dictionary)new Codec().Decode(buffer);
            var block = BlockMarshaler.UnmarshalBlock(dict);
            receiver.Tip = block;
        }
    }

    private class TestService : ServiceBase<IBlockChainService>, IBlockChainService
    {
        private IWorld _states;

        public TestService()
        {
            _states = new World(MockWorldState.CreateModern());
        }

        public IBlockChainService WithOptions(CallOptions option)
        {
            throw new NotImplementedException();
        }

        public IBlockChainService WithHeaders(Metadata headers)
        {
            throw new NotImplementedException();
        }

        public IBlockChainService WithDeadline(DateTime deadline)
        {
            throw new NotImplementedException();
        }

        public IBlockChainService WithCancellationToken(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public IBlockChainService WithHost(string host)
        {
            throw new NotImplementedException();
        }

        public UnaryResult<bool> PutTransaction(byte[] txBytes)
        {
            throw new NotImplementedException();
        }

        public UnaryResult<long> GetNextTxNonce(byte[] addressBytes)
        {
            throw new NotImplementedException();
        }

        public UnaryResult<byte[]> GetStateByBlockHash(
            byte[] blockHashBytes,
            byte[] accountBytes,
            byte[] addressBytes)
        {
            var address = new Address(addressBytes);
            var value = _states.GetLegacyState(address);
            if (value is null) throw new NullReferenceException();
            return new UnaryResult<byte[]>(new Codec().Encode(value));
        }

        public UnaryResult<byte[]> GetStateByStateRootHash(
            byte[] stateRootHashBytes,
            byte[] accountBytes,
            byte[] addressBytes) =>
            GetStateByBlockHash(stateRootHashBytes, accountBytes, addressBytes);

        public UnaryResult<byte[]> GetBalanceByBlockHash(
            byte[] blockHashBytes,
            byte[] addressBytes,
            byte[] currencyBytes) =>
            throw new NotImplementedException();

        public UnaryResult<byte[]> GetBalanceByStateRootHash(
            byte[] stateRootHashBytes,
            byte[] addressBytes,
            byte[] currencyBytes) =>
            throw new NotImplementedException();

        public UnaryResult<byte[]> GetTip() => throw new NotImplementedException();

        public UnaryResult<byte[]> GetBlockHash(long blockIndex) => throw new NotImplementedException();

        public UnaryResult<bool> SetAddressesToSubscribe(byte[] toByteArray, IEnumerable<byte[]> addressesBytes) =>
            throw new NotImplementedException();

        public UnaryResult<bool> IsTransactionStaged(byte[] txidBytes) => throw new NotImplementedException();

        public UnaryResult<bool> ReportException(string code, string message) => throw new NotImplementedException();

        public UnaryResult<bool> AddClient(byte[] addressByte) => throw new NotImplementedException();

        public UnaryResult<bool> RemoveClient(byte[] addressByte) => throw new NotImplementedException();

        public UnaryResult<Dictionary<byte[], byte[]>> GetAgentStatesByBlockHash(
            byte[] blockHashBytes,
            IEnumerable<byte[]> addressBytesList)
        {
            var result = new Dictionary<byte[], byte[]>();
            foreach (var addressBytes in addressBytesList)
            {
                var address = new Address(addressBytes);
                var value = _states.GetResolvedState(address, Addresses.Agent);
                if (value is { } iValue)
                {
                    result.Add(addressBytes, new Codec().Encode(iValue));
                }
            }

            return new UnaryResult<Dictionary<byte[], byte[]>>(result);
        }

        public UnaryResult<Dictionary<byte[], byte[]>> GetAgentStatesByStateRootHash(
            byte[] stateRootHashBytes,
            IEnumerable<byte[]> addressBytesList) => GetAgentStatesByBlockHash(stateRootHashBytes, addressBytesList);

        public UnaryResult<Dictionary<byte[], byte[]>> GetAvatarStatesByBlockHash(
            byte[] blockHashBytes,
            IEnumerable<byte[]> addressBytesList) =>
            throw new NotImplementedException();

        public UnaryResult<Dictionary<byte[], byte[]>> GetAvatarStatesByStateRootHash(
            byte[] stateRootHashBytes,
            IEnumerable<byte[]> addressBytesList) =>
            throw new NotImplementedException();

        public UnaryResult<Dictionary<byte[], byte[]>> GetBulkStateByBlockHash(
            byte[] blockHashBytes,
            byte[] accountBytes,
            IEnumerable<byte[]> addressBytesList)
        {
            var result = new Dictionary<byte[], byte[]>();
            foreach (var addressBytes in addressBytesList)
                try
                {
                    result[addressBytes] =
                        GetStateByBlockHash(blockHashBytes, accountBytes, addressBytes).ResponseAsync.Result;
                }
                catch (NullReferenceException)
                {
                }

            return new UnaryResult<Dictionary<byte[], byte[]>>(result);
        }

        public UnaryResult<Dictionary<byte[], byte[]>> GetBulkStateByStateRootHash(
            byte[] stateRootHashBytes,
            byte[] accountBytes,
            IEnumerable<byte[]> addressBytesList) =>
            GetBulkStateByBlockHash(stateRootHashBytes, accountBytes, addressBytesList);

        public UnaryResult<Dictionary<byte[], byte[]>> GetSheets(
            byte[] blockHashBytes,
            IEnumerable<byte[]> addressBytesList) =>
            throw new NotImplementedException();

        public void SetOrder(Order order)
        {
            SetState(Order.DeriveAddress(order.OrderId), order.Serialize());
        }

        public void SetState(Address address, IValue value)
        {
            _states = _states.SetLegacyState(address, value);
        }

        public void SetAgentState(Address address, AgentState agentState)
        {
            _states = _states.SetAgentState(address, agentState);
        }
    }

    private void SetShopStates(ItemSubType itemSubType, OrderDigest orderDigest)
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

        foreach (var type in itemSubTypes)
        {
            var shopAddresses = _client.GetShopAddress(type).Select(a => new Address(a)).ToArray();
            foreach (var shopAddress in shopAddresses)
            {
                var shopState = new ShardedShopStateV2(shopAddress);
                if (type == itemSubType && shopAddress == ShardedShopStateV2.DeriveAddress(type, orderDigest.OrderId))
                {
                    shopState.Add(orderDigest, 0L);
                }
                _testService.SetState(shopAddress, shopState.Serialize());
            }
        }
    }
}
