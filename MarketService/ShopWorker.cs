using System.Diagnostics;
using Nekoyume.Model.Item;
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
            foreach (var itemSubType in itemSubTypes)
            {
                try
                {
                    await _rpcClient.SyncOrder(itemSubType, hashBytes, crystalEquipmentGrindingSheet,
                        crystalMonsterCollectionMultiplierSheet);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "error occured");
                }

                await Task.Delay(100, stoppingToken);
            }

            stopWatch.Stop();
            var ts = stopWatch.Elapsed;
            _logger.LogInformation("Complete sync shop. {TotalElapsed}", ts);
            await Task.Delay(3000, stoppingToken);
        }
    }
}
