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
        await _rpcClient.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _rpcClient.StopAsync(cancellationToken);
    }
}
