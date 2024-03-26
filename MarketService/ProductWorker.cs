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
        if (!_rpcClient.Init)
        {
#pragma warning disable CS4014
            _rpcClient.StartAsync(stoppingToken);
#pragma warning restore CS4014
        }

        while (true)
        {
            if (stoppingToken.IsCancellationRequested) stoppingToken.ThrowIfCancellationRequested();

            var stopWatch = new Stopwatch();
            _logger.LogInformation("[ProductWorker]Start sync product");
            stopWatch.Start();

            while (!_rpcClient.Init) await Task.Delay(100, stoppingToken);

            try
            {
                var hashBytes = await _rpcClient.GetBlockHashBytes();
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
            _logger.LogInformation("[ProductWorker]Complete sync product. {TotalElapsed}", ts);
            await Task.Delay(1000, stoppingToken);
        }
    }
}
