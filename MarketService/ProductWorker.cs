using System.Diagnostics;
using Nekoyume.TableData;
using Nekoyume.TableData.Crystal;

namespace MarketService;

public class ProductWorker : BackgroundService
{
    private readonly ILogger<ProductWorker> _logger;
    private readonly RpcClient _rpcClient;

    public ProductWorker(ILogger<ProductWorker> logger, RpcClient client)
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
            _logger.LogInformation("[ProductWorker]Start sync product");

            while (_rpcClient.Tip is null) await Task.Delay(100, stoppingToken);

            stopWatch.Start();

            try
            {
                var hashBytes = await _rpcClient.GetBlockStateRootHashBytes();
                var crystalEquipmentGrindingSheet = await _rpcClient.GetSheet<CrystalEquipmentGrindingSheet>(hashBytes);
                var crystalMonsterCollectionMultiplierSheet =
                    await _rpcClient.GetSheet<CrystalMonsterCollectionMultiplierSheet>(hashBytes);
                var costumeStatSheet = await _rpcClient.GetSheet<CostumeStatSheet>(hashBytes);
                await _rpcClient.SyncProduct(hashBytes, crystalEquipmentGrindingSheet, crystalMonsterCollectionMultiplierSheet, costumeStatSheet);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "error occured");
            }

            stopWatch.Stop();
            var ts = stopWatch.Elapsed;
            await Task.Delay(1000, stoppingToken);
            _logger.LogInformation("[ProductWorker]Complete sync product on {BlockIndex}. {TotalElapsed}", _rpcClient.Tip.Index, ts);
        }
    }
}
