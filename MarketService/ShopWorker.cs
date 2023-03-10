using System.Collections.Concurrent;
using System.Diagnostics;
using Lib9c.Model.Order;
using Nekoyume.Model.Item;
using Nekoyume.TableData;
using Nekoyume.TableData.Crystal;

namespace MarketService;

public class ShopWorker : BackgroundService
{
    private readonly ILogger<ShopWorker> _logger;
    private readonly RpcClient _rpcClient;


    public ShopWorker(ILogger<ShopWorker> logger, RpcClient client)
    {
        _logger = logger;
        _rpcClient = client;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
#pragma warning disable CS4014
        _rpcClient.StartAsync(stoppingToken);
#pragma warning restore CS4014
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
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = 8
        };
        while (true)
        {
            if (stoppingToken.IsCancellationRequested) stoppingToken.ThrowIfCancellationRequested();

            var stopWatch = new Stopwatch();
            _logger.LogInformation("Start sync shop");
            stopWatch.Start();

            while (!_rpcClient.Init) await Task.Delay(100, stoppingToken);

            var hashBytes = await _rpcClient.GetBlockHashBytes();
            var crystalEquipmentGrindingSheet = await _rpcClient.GetSheet<CrystalEquipmentGrindingSheet>(hashBytes);
            var crystalMonsterCollectionMultiplierSheet =
                await _rpcClient.GetSheet<CrystalMonsterCollectionMultiplierSheet>(hashBytes);
            var costumeStatSheet = await _rpcClient.GetSheet<CostumeStatSheet>(hashBytes);
            var chainIds = new ConcurrentBag<Guid>();
            var orderDigestList = new ConcurrentBag<OrderDigest>();
            await Parallel.ForEachAsync(itemSubTypes, parallelOptions, async (itemSubType, st) =>
            {
                var list = await _rpcClient.GetOrderDigests(itemSubType, hashBytes);

                foreach (var digest in list)
                {
                    chainIds.Add(digest.OrderId);
                    orderDigestList.Add(digest);
                }
            });
            await _rpcClient.SyncOrder(chainIds.ToList(), orderDigestList.ToList(), hashBytes,
                crystalEquipmentGrindingSheet,
                crystalMonsterCollectionMultiplierSheet, costumeStatSheet);

            stopWatch.Stop();
            var ts = stopWatch.Elapsed;
            _logger.LogInformation("Complete sync shop({TotalCount}). {TotalElapsed}", orderDigestList.Count, ts);
            await Task.Delay(3000, stoppingToken);
        }
    }
}
