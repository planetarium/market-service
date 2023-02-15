namespace MarketService;

public class RpcConfigOptions
{
    public const string RpcConfig = "RpcConfig";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
}