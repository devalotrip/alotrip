using Flight.Domain.Enums;

namespace Flight.Application.Dtos;

public sealed class SearchFlightRequest
{
    public string   Origin      { get; set; } = default!;
    public string   Destination { get; set; } = default!;
    public DateTime DepartDate  { get; set; }
    public DateTime? ReturnDate { get; set; }
    public TripType TripType    { get; set; }
    public int AdultCount       { get; set; } = 1;
    public int ChildCount       { get; set; }
    public int InfantCount      { get; set; }
    public string Currency      { get; set; } = "VND";
    public string? AgentCode    { get; set; }
}

public sealed class BookFlightRequest
{
    public string   FareId       { get; set; } = default!;
    public string   SessionData  { get; set; } = default!;
    public string   AgentCode    { get; set; } = default!;
    public string   ContactName  { get; set; } = default!;
    public string   ContactEmail { get; set; } = default!;
    public string   ContactPhone { get; set; } = default!;
    public List<PassengerBookingDto> Passengers { get; set; } = [];

    // ── Optional booking add-ons (migrated from old BookWithDetail SOAP) ─────
    public InvoiceBookingDto? Invoice { get; set; }
    public List<CarRentalBookingDto>? CarRentals { get; set; }
    public List<TripTourBookingDto>?  TripTours { get; set; }
    public List<TripVisaBookingDto>?  TripVisas { get; set; }
    public TripCancellationBookingDto? TripCancellation { get; set; }
}

public sealed class PassengerBookingDto
{
    public string       FirstName   { get; set; } = default!;
    public string       LastName    { get; set; } = default!;
    public string?      MiddleName  { get; set; }
    public string       Gender      { get; set; } = default!;
    public PassengerType Type       { get; set; }
    public DateOnly?    BirthDate   { get; set; }
    public string?      PassportNo  { get; set; }
    public DateOnly?    PassportExpiry { get; set; }
    public string?      Nationality { get; set; }
    public decimal      BaggageKg   { get; set; }
}

public sealed class BookResultDto
{
    public bool   IsSuccess    { get; set; }
    public string BookingCode  { get; set; } = default!;
    public string? PnrCode     { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public decimal TotalAmount { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class IssueTicketResultDto
{
    public bool   IsSuccess   { get; set; }
    public List<string> TicketNumbers { get; set; } = [];
    public string? ErrorMessage { get; set; }
}

public sealed class RebookResultDto
{
    public Guid    NewBookingId { get; init; }
    public decimal OldPrice    { get; init; }
    public decimal NewPrice    { get; init; }
    /// <summary>Positive = price went up; Negative = price went down.</summary>
    public decimal PriceDiff   => NewPrice - OldPrice;
    public string  Currency    { get; init; } = default!;
}

// ── Booking Add-on DTOs (submitted with BookFlightRequest) ───────────────────

/// <summary>
/// Invoice data attached to booking. Map from old Invoice model.
/// </summary>
public sealed class InvoiceBookingDto
{
    public string  CompanyName   { get; set; } = default!;
    public string? Address       { get; set; }
    public string? CityName      { get; set; }
    public string? TaxCode       { get; set; }
    public string? Receiver      { get; set; }
    public string? ReceiverPhone { get; set; }
    public string? ReceiverEmail { get; set; }
}

/// <summary>
/// Car rental attached to booking. Simplified from old tblCarRental.
/// </summary>
public sealed class CarRentalBookingDto
{
    public string   Provider        { get; set; } = default!;
    public string   PickupLocation  { get; set; } = default!;
    public string   DropoffLocation { get; set; } = default!;
    public DateTime PickupDate      { get; set; }
    public DateTime DropoffDate     { get; set; }
    public string   CarType         { get; set; } = default!;
    public string?  CarModel        { get; set; }
    public decimal  DailyRate       { get; set; }
    public string   Currency        { get; set; } = "VND";
}

/// <summary>
/// Trip tour attached to booking. Map from old tblTripTour (Code, Name, Price, Value, Currency).
/// </summary>
public sealed class TripTourBookingDto
{
    public string   Code     { get; set; } = default!;
    public string   Name     { get; set; } = default!;
    public decimal  Price    { get; set; }
    public string?  Value    { get; set; }
    public string   Currency { get; set; } = "VND";
}

/// <summary>
/// Trip visa attached to booking. Map from old tblTripVisa (Code, Name, Price, Value, Currency).
/// </summary>
public sealed class TripVisaBookingDto
{
    public string   Code     { get; set; } = default!;
    public string   Name     { get; set; } = default!;
    public decimal? Price    { get; set; }
    public string?  Value    { get; set; }
    public string   Currency { get; set; } = "VND";
}

/// <summary>
/// Trip cancellation protection attached to booking. Map from old TripCancellation.
/// </summary>
public sealed class TripCancellationBookingDto
{
    public decimal  MarkupAmount  { get; set; }
    public decimal  MarkupPercent { get; set; }
    public decimal  Price         { get; set; }
    public string   Currency      { get; set; } = "VND";
    public decimal  BookingPrice  { get; set; }
    public string?  Value         { get; set; }
}
