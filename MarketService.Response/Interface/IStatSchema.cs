using Nekoyume.Model.Stat;

namespace MarketService.Response.Interface;

public interface IStatSchema
{
    public long Value { get; set; }
    public StatType Type { get; set; }
    public bool Additional { get; set; }
}
