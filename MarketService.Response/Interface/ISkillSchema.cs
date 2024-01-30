using Nekoyume.Model.Elemental;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;

namespace MarketService.Response.Interface;

public interface ISkillSchema
{
    public int SkillId { get; set; }
    public ElementalType ElementalType { get; set; }
    public SkillCategory SkillCategory { get; set; }
    public int HitCount { get; set; }
    public int Cooldown { get; set; }
    public long Power { get; set; }
    public int StatPowerRatio { get; set; }
    public int Chance { get; set; }
    public StatType ReferencedStatType { get; set; }
}
