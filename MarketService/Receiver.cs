using Bencodex;
using Bencodex.Types;
using Libplanet.Types.Blocks;
using Nekoyume.Shared.Hubs;

namespace MarketService;

public class Receiver : IActionEvaluationHubReceiver
{
    public Block Tip;
    public Block PreviousTip;
    private readonly ILogger<Receiver> _logger;
    private readonly Codec _codec = new Codec();

    public Receiver(ILogger<Receiver> logger)
    {
        _logger = logger;
    }

    public void OnRender(byte[] evaluation)
    {
    }

    public void OnUnrender(byte[] evaluation)
    {
    }

    public void OnRenderBlock(byte[] oldTip, byte[] newTip)
    {
        var dict = (Dictionary)_codec.Decode(newTip);
        var newTipBlock = BlockMarshaler.UnmarshalBlock(dict);
        PreviousTip = Tip;
        Tip = newTipBlock;
    }

    public void OnReorged(byte[] oldTip, byte[] newTip, byte[] branchpoint)
    {
    }

    public void OnReorgEnd(byte[] oldTip, byte[] newTip, byte[] branchpoint)
    {
    }

    public void OnException(int code, string message)
    {
    }

    public void OnPreloadStart()
    {
    }

    public void OnPreloadEnd()
    {
    }
}
