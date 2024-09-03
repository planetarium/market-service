using System.IO.Compression;
using Bencodex;
using Bencodex.Types;
using Lib9c.Renderers;
using Libplanet.Types.Blocks;
using MessagePack;
using Microsoft.Extensions.Options;
using Nekoyume.Action;
using Nekoyume.Shared.Hubs;

namespace MarketService;

public class Receiver : IActionEvaluationHubReceiver
{
    public Block Tip;
    public Block PreviousTip;
    private readonly ILogger<Receiver> _logger;
    private readonly Codec _codec = new Codec();
    private readonly ActionRenderer _actionRenderer;
    private readonly WorkerOptions _workerOptions;

    public Receiver(ILogger<Receiver> logger, ActionRenderer actionRenderer, IOptions<WorkerOptions> options)
    {
        _logger = logger;
        _actionRenderer = actionRenderer;
        _workerOptions = options.Value;
    }

    public void OnRender(byte[] evaluation)
    {
        if (_workerOptions.SyncProduct || _workerOptions.SyncShop)
        {
            using (var cp = new MemoryStream(evaluation))
            {
                using (var decompressed = new MemoryStream())
                {
                    using (var df = new DeflateStream(cp, CompressionMode.Decompress))
                    {
                        df.CopyTo(decompressed);
                        decompressed.Seek(0, SeekOrigin.Begin);
                        var dec = decompressed.ToArray();
                        var ev = MessagePackSerializer.Deserialize<NCActionEvaluation>(dec)
                            .ToActionEvaluation();
                        _actionRenderer.ActionRenderSubject.OnNext(ev);
                    }
                }
            }
        }
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
