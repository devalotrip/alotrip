using Flight.Domain.Enums;

namespace Flight.Application.Dtos;

/// <summary>
/// Kết quả tìm kiếm chuyến bay. Map từ FareData + FareDataWithServiceFee cũ.
/// </summary>
public sealed class FareDataDto
{
    public string        FareId       { get; set; } = default!;
    public FlightSource  Source       { get; set; }
    public string        Airline      { get; set; } = default!;
    public string        Origin       { get; set; } = default!;
    public string        Destination  { get; set; } = default!;
    public DateTime      DepartDate   { get; set; }
    public DateTime?     ReturnDate   { get; set; }
    public TripType      TripType     { get; set; }

    // Pax counts
    public int AdultCount  { get; set; }
    public int ChildCount  { get; set; }
    public int InfantCount { get; set; }

    // Pricing per pax
    public decimal AdultFare   { get; set; }
    public decimal ChildFare   { get; set; }
    public decimal InfantFare  { get; set; }
    public decimal TaxAmount   { get; set; }
    public decimal ServiceFee  { get; set; }
    public decimal TotalFare   { get; set; }
    public string  Currency    { get; set; } = default!;

    // Engine session data (cần khi book)
    public string? SessionData { get; set; }
    public string? PccCode     { get; set; }

    public List<FlightSegmentDto> OutboundSegments { get; set; } = [];
    public List<FlightSegmentDto> ReturnSegments   { get; set; } = [];

    public DateTime CachedAt  { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public sealed class FlightSegmentDto
{
    public string   FlightNumber  { get; set; } = default!;
    public string   Airline       { get; set; } = default!;
    public string   Origin        { get; set; } = default!;
    public string   Destination   { get; set; } = default!;
    public DateTime DepartTime    { get; set; }
    public DateTime ArriveTime    { get; set; }
    public string   CabinClass    { get; set; } = default!;
    public string?  AircraftType  { get; set; }
    public int      StopCount     { get; set; }

    /// <summary>
    /// Engine-specific per-segment booking token / FlightValue.
    /// Used by Datacom (FlightValue), Maybay (SelectValue) when submitting a book request.
    /// </summary>
    public string?  SelectedValue { get; set; }
}

// ── Baggage DTOs ─────────────────────────────────────────────────────────────

/// <summary>
/// Thông tin hành lý mua thêm cho chuyến đi + chuyến về.
/// Map từ BaggageInfo (code cũ).
/// </summary>
public sealed class BaggageInfoDto
{
    public List<BaggageOptionDto> DepartBaggages  { get; set; } = [];
    public List<BaggageOptionDto> ReturnBaggages  { get; set; } = [];
}

public sealed class BaggageOptionDto
{
    public string  AirlineCode { get; set; } = default!;
    public string  Code        { get; set; } = default!;  // booking code để select
    public string  Name        { get; set; } = default!;  // e.g. "20 kg", "1 piece"
    public string  Value       { get; set; } = default!;  // engine-specific value
    public decimal Price       { get; set; }
    public string  Currency    { get; set; } = default!;
}

// ── FareRule DTOs ─────────────────────────────────────────────────────────────

/// <summary>
/// Nhóm điều kiện vé (e.g. "Quy định hoàn vé", "Quy định đổi vé").
/// Map từ RulesGroup (code cũ).
/// </summary>
public sealed class FareRuleGroupDto
{
    public string             Title { get; set; } = default!;
    public List<string>       Rules { get; set; } = [];
}
