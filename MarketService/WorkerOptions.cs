namespace MarketService;

public class WorkerOptions
{
    public const string WorkerConfig = "WorkerConfig";

    public bool SyncShop { get; set; }
    public bool SyncProduct { get; set; }
}
