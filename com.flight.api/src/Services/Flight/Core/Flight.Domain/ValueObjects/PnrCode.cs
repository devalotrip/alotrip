using Flight.Domain.Exceptions;

namespace Flight.Domain.ValueObjects;

/// <summary>
/// Mã đặt chỗ PNR (Passenger Name Record).
/// Map từ trường BookingCode/PNR trong tblBooking cũ.
/// </summary>
public sealed record PnrCode
{
    public string Value { get; }

    private PnrCode(string value) => Value = value;

    public static PnrCode Of(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("PNR code cannot be empty.");
        if (code.Length is < 5 or > 10)
            throw new DomainException("PNR code must be between 5 and 10 characters.");

        return new PnrCode(code.ToUpperInvariant());
    }

    public override string ToString() => Value;
}
