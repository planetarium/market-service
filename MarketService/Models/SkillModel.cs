using Nekoyume.Model.Elemental;
using Nekoyume.Model.Skill;

namespace MarketService.Models;

public class SkillModel
{
    public int SkillId { get; set; }
    public ElementalType ElementalType { get; set; }
    public SkillCategory SkillCategory { get; set; }
    public int HitCount { get; set; }
    public int Cooldown { get; set; }
    public int Power { get; set; }
    public int Chance { get; set; }
}