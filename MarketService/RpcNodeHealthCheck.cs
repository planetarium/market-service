using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MarketService;

public class RpcNodeHealthCheck : IHealthCheck
{
    private volatile bool _ready;

    public bool ConnectCompleted
    {
        get => _ready;
        set => _ready = value;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        if (ConnectCompleted)
        {
            return Task.FromResult(HealthCheckResult.Healthy("grpc connect completed"));
        }

        return Task.FromResult(HealthCheckResult.Unhealthy("grpc connect not completed"));
    }
}
