using HotChocolate.Language;
using Libplanet.Crypto;

namespace MarketService.GraphTypes;

public class AddressType : ScalarType<Address, StringValueNode>
{
    public AddressType() : base("address")
    {
    }

    public override IValueNode ParseResult(object? resultValue)
    {
        return ParseValue(resultValue);
    }

    protected override Address ParseLiteral(StringValueNode valueSyntax)
    {
        return new Address(valueSyntax.Value);
    }

    protected override StringValueNode ParseValue(Address runtimeValue)
    {
        return new(Serialize(runtimeValue));
    }

    private static string Serialize(Address runtimeValue)
    {
        return runtimeValue.ToString();
    }

    public override bool TrySerialize(object? runtimeValue, out object? resultValue)
    {
        if (runtimeValue is null)
        {
            resultValue = null;
            return true;
        }

        if (runtimeValue is Address a)
        {
            resultValue = Serialize(a);
            return true;
        }

        resultValue = null;
        return false;
    }

    public override bool TryDeserialize(object? resultValue, out object? runtimeValue)
    {
        if (resultValue is null)
        {
            runtimeValue = null;
            return true;
        }

        if (resultValue is Address)
        {
            runtimeValue = resultValue;
            return true;
        }

        runtimeValue = null;
        return false;
    }
}
