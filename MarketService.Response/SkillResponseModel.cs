using MarketService.Response.Interface;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Skill;

namespace MarketService.Response
{
    public class SkillResponseModel: ISkillSchema
    {
        public int SkillId { get; set; }
        public ElementalType ElementalType { get; set; }
        public SkillCategory SkillCategory { get; set; }
        public int HitCount { get; set; }
        public int Cooldown { get; set; }
        public int Power { get; set; }
        public int Chance { get; set; }
    }
}
