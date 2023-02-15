using Nekoyume.Model.Stat;

namespace MarketService.Models;

public class StatModel
{
    public int Value { get; set; }
    public StatType Type { get; set; }
    public bool Additional { get; set; }
}