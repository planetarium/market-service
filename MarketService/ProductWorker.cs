using Nekoyume.TableData.Crystal;

namespace MarketService;

public class ProductWorker : BackgroundService
{
    private readonly ILogger<ShopWorker> _logger;
    private readonly RpcClient _rpcClient;

    public ProductWorker(ILogger<ShopWorker> logger, RpcClient client)
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

            while (!_rpcClient.Init) await Task.Delay(100, stoppingToken);

            try
            {
                var hashBytes = await _rpcClient.GetBlockHashBytes();
                var crystalEquipmentGrindingSheet = await _rpcClient.GetSheet<CrystalEquipmentGrindingSheet>(hashBytes);
                var crystalMonsterCollectionMultiplierSheet =
                    await _rpcClient.GetSheet<CrystalMonsterCollectionMultiplierSheet>(hashBytes);
                await _rpcClient.SyncProduct(hashBytes, crystalEquipmentGrindingSheet, crystalMonsterCollectionMultiplierSheet);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "error occured");
            }

            await Task.Delay(1000, stoppingToken);
        }
    }
}
