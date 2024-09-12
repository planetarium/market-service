using MarketService.Models;

namespace MarketService.GraphTypes;

public class SkillModelType : ObjectType<SkillModel>
{
    protected override void Configure(IObjectTypeDescriptor<SkillModel> descriptor)
    {
        descriptor.Ignore(f => f.ToResponse());
    }
}
