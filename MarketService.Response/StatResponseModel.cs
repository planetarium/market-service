using MarketService.Response.Interface;
using Nekoyume.Model.Stat;

namespace MarketService.Response;

public class StatResponseModel : IStatSchema
{
    public int Value { get; set; }
    public StatType Type { get; set; }
    public bool Additional { get; set; }
}
