using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MarketService;

public class AddressConverter : ValueConverter<Address, string>
{
    public AddressConverter() : base(v => v.ToHex(), v => new Address(v))
    {
    }
}