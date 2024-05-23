using MarketService.Models;

namespace MarketService.GraphTypes;

public class StatModelType: ObjectType<StatModel>
{
    protected override void Configure(IObjectTypeDescriptor<StatModel> descriptor)
    {
        descriptor.Ignore(f => f.ToResponse());
    }
}
