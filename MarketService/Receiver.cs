using Bencodex;
using Bencodex.Types;
using Libplanet.Types.Blocks;
using Nekoyume.Shared.Hubs;

namespace MarketService;

public class Receiver : IActionEvaluationHubReceiver
{
    public Block Tip;
    private readonly ILogger<Receiver> _logger;
    private readonly Codec _codec = new Codec();

    public Receiver(ILogger<Receiver> logger)
    {
        _logger = logger;
    }

    public void OnRender(byte[] evaluation)
    {
        _logger.LogDebug("Start {Method}", nameof(OnRender));
    }

    public void OnUnrender(byte[] evaluation)
    {
        _logger.LogDebug("Start {Method}", nameof(OnUnrender));
    }

    public void OnRenderBlock(byte[] oldTip, byte[] newTip)
    {
        var dict = (Dictionary)_codec.Decode(newTip);
        var newTipBlock = BlockMarshaler.UnmarshalBlock(dict);
        Tip = newTipBlock;
    }

    public void OnReorged(byte[] oldTip, byte[] newTip, byte[] branchpoint)
    {
        _logger.LogDebug("Start {Method}", nameof(OnReorged));
    }

    public void OnReorgEnd(byte[] oldTip, byte[] newTip, byte[] branchpoint)
    {
        _logger.LogDebug("Start {Method}", nameof(OnReorgEnd));
    }

    public void OnException(int code, string message)
    {
        _logger.LogDebug("Start {Method}", nameof(OnException));
    }

    public void OnPreloadStart()
    {
        _logger.LogDebug("Start {Method}", nameof(OnPreloadStart));
    }

    public void OnPreloadEnd()
    {
        _logger.LogDebug("Start {Method}", nameof(OnPreloadEnd));
    }
}
