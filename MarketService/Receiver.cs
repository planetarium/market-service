using System.Diagnostics;
using Bencodex;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Blocks;
using Nekoyume.Action;
using Nekoyume.Shared.Hubs;

namespace MarketService;

public class Receiver : IActionEvaluationHubReceiver
{
    private readonly ILogger<Receiver> _logger;

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
        var stopWatch = new Stopwatch();
        _logger.LogDebug("Start {Method}", nameof(OnRenderBlock));
        stopWatch.Start();
        var codec = new Codec();
        var oldBlock = BlockMarshaler.UnmarshalBlock<PolymorphicAction<ActionBase>>((Dictionary) codec.Decode(oldTip));
        var newBlock = BlockMarshaler.UnmarshalBlock<PolymorphicAction<ActionBase>>((Dictionary) codec.Decode(newTip));
        stopWatch.Stop();
        var ts = stopWatch.Elapsed;

        // _logger.LogInformation(
        //     "Block render from {OldTipIndex} to {NewTipIndex} at {TimeTaken}",
        //     oldBlock.Index,
        //     newBlock.Index,
        //                 ts);
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