using Flight.Domain.Enums;
using Shared.BuildingBlocks.Abstractions;

namespace Flight.Domain.Aggregates.Booking;

/// <summary>
/// Hành khách trong booking. Map từ tblPassenger cũ.
/// </summary>
public sealed class PassengerEntity : Entity<Guid>
{
    public Guid          BookingId     { get; private set; }
    public string        FirstName     { get; private set; } = default!;
    public string        LastName      { get; private set; } = default!;
    public string?       MiddleName    { get; private set; }
    public string        Gender        { get; private set; } = default!;
    public DateOnly?     BirthDate     { get; private set; }
    public PassengerType Type          { get; private set; }
    public string?       PassportNo    { get; private set; }
    public DateOnly?     PassportExpiry { get; private set; }
    public string?       Nationality   { get; private set; }
    public decimal       BaggageKg     { get; private set; }
    public decimal       FareAmount    { get; private set; }
    public string        FareCurrency  { get; private set; } = default!;

    private PassengerEntity() { }

    public static PassengerEntity Create(
        Guid bookingId,
        string firstName, string lastName, string gender,
        PassengerType type, decimal fareAmount, string fareCurrency,
        string? middleName = null, DateOnly? birthDate = null,
        string? passportNo = null, DateOnly? passportExpiry = null,
        string? nationality = null, decimal baggageKg = 0)
    {
        return new PassengerEntity
        {
            Id              = Guid.NewGuid(),
            BookingId       = bookingId,
            FirstName       = firstName,
            LastName        = lastName,
            MiddleName      = middleName,
            Gender          = gender,
            BirthDate       = birthDate,
            Type            = type,
            PassportNo      = passportNo,
            PassportExpiry  = passportExpiry,
            Nationality     = nationality,
            BaggageKg       = baggageKg,
            FareAmount      = fareAmount,
            FareCurrency    = fareCurrency,
            CreatedOnUtc    = DateTime.UtcNow
        };
    }

    public string FullName => string.IsNullOrEmpty(MiddleName)
        ? $"{LastName} {FirstName}"
        : $"{LastName} {MiddleName} {FirstName}";
}
