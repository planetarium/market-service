using Nekoyume.Model.Stat;

namespace MarketService.Response.Interface;

public interface IStatModel
{
    public int Value { get; set; }
    public StatType Type { get; set; }
    public bool Additional { get; set; }

    public StatResponseModel ToResponse();
}
