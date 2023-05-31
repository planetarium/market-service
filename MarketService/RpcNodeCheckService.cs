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
            _healthCheck.ConnectCompleted = _rpcClient.Ready;
            await Task.Delay(3000, stoppingToken);
        }
    }
}
