using Nekoyume.Model.Stat;

namespace MarketService.Response.Interface;

public interface IStatModel
{
    public long Value { get; set; }
    public StatType Type { get; set; }
    public bool Additional { get; set; }

    public StatResponseModel ToResponse();
}
