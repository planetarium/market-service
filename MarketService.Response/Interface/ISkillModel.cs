using Nekoyume.Model.Elemental;
using Nekoyume.Model.Skill;

namespace MarketService.Response.Interface;

public interface ISkillModel
{
    public SkillResponseModel ToResponse();
}
