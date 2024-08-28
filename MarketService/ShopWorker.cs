using System.Collections.Concurrent;
using System.Diagnostics;
using Lib9c.Model.Order;
using Libplanet;
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
        while (true)
        {
            if (stoppingToken.IsCancellationRequested) stoppingToken.ThrowIfCancellationRequested();

            var stopWatch = new Stopwatch();

            while (_rpcClient.Tip is null) await Task.Delay(100, stoppingToken);
            _logger.LogInformation("Start sync shop");
            stopWatch.Start();

            var hashBytes = await _rpcClient.GetBlockStateRootHashBytes();
            var crystalEquipmentGrindingSheet = await _rpcClient.GetSheet<CrystalEquipmentGrindingSheet>(hashBytes);
            var crystalMonsterCollectionMultiplierSheet =
                await _rpcClient.GetSheet<CrystalMonsterCollectionMultiplierSheet>(hashBytes);
            var costumeStatSheet = await _rpcClient.GetSheet<CostumeStatSheet>(hashBytes);
            await _rpcClient.SyncOrder(hashBytes,
                crystalEquipmentGrindingSheet,
                crystalMonsterCollectionMultiplierSheet, costumeStatSheet);

            stopWatch.Stop();
            var ts = stopWatch.Elapsed;
            _logger.LogInformation("Complete sync shop on {BlockIndex}. {TotalElapsed}", _rpcClient.Tip, ts);
            await Task.Delay(8000, stoppingToken);
        }
    }
}
