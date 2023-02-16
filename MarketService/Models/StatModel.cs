using MarketService.Response;
using MarketService.Response.Interface;
using Nekoyume.Model.Stat;

namespace MarketService.Models;

public class StatModel: IStatModel
{
    public int Value { get; set; }
    public StatType Type { get; set; }
    public bool Additional { get; set; }
    public StatResponseModel ToResponse()
    {
        return new StatResponseModel
        {
            Value = Value,
            Type = Type,
            Additional = Additional,
        };
    }
}
