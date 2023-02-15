using MarketService.Response;
using MarketService.Response.Interface;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Skill;

namespace MarketService.Models;

public class SkillModel: ISkillSchema, ISkillModel
{
    public int SkillId { get; set; }
    public ElementalType ElementalType { get; set; }
    public SkillCategory SkillCategory { get; set; }
    public int HitCount { get; set; }
    public int Cooldown { get; set; }
    public int Power { get; set; }
    public int Chance { get; set; }
    public SkillResponseModel ToResponse()
    {
        return new SkillResponseModel
        {
            SkillCategory = SkillCategory,
            HitCount = HitCount,
            Cooldown = Cooldown,
            Power = Power,
            Chance = Chance,
            ElementalType = ElementalType,
            SkillId = SkillId,
        };
    }
}
