namespace MarketService;

public class RpcService : IHostedService
{
    private readonly RpcClient _rpcClient;

    public RpcService(RpcClient rpcClient)
    {
        _rpcClient = rpcClient;
    }
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _ = _rpcClient.StartAsync(cancellationToken);
        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _rpcClient.StopAsync(cancellationToken);
    }
}
