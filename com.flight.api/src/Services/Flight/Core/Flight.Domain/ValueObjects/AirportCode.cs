using Flight.Domain.Exceptions;

namespace Flight.Domain.ValueObjects;

/// <summary>
/// Mã sân bay IATA 3 ký tự. Ví dụ: SGN, HAN, DAD
/// </summary>
public sealed record AirportCode
{
    public string Value { get; }

    private AirportCode(string value) => Value = value;

    public static AirportCode Of(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != 3)
            throw new DomainException($"Invalid airport code: '{code}'. Must be 3 letters (IATA).");

        return new AirportCode(code.ToUpperInvariant());
    }

    public override string ToString() => Value;
}
