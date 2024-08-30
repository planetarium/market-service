namespace MarketService;

public class RpcNodeCheckService : BackgroundService
{
    private readonly RpcNodeHealthCheck _healthCheck;
    private readonly RpcClient _rpcClient;

    public RpcNodeCheckService(RpcNodeHealthCheck healthCheck, RpcClient rpcClient)
    {
        _healthCheck = healthCheck;
        _rpcClient = rpcClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            if (stoppingToken.IsCancellationRequested) stoppingToken.ThrowIfCancellationRequested();

            while (_rpcClient.Tip is null) await Task.Delay(100, stoppingToken);
            _healthCheck.ConnectCompleted = _rpcClient.Ready;
            await Task.Delay(3000, stoppingToken);
        }
    }
}
