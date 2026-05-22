using Flight.Domain.Enums;

namespace Flight.Application.Dtos;

/// <summary>
/// Kết quả tìm kiếm chuyến bay. Map từ FareData + FareDataWithServiceFee cũ.
///
/// Cấu trúc nested giống code cũ:
///   FareDataDto
///     ├─ OutboundOptions: List<FlightOptionDto>  (tương đương ListDepartureFlight)
///     │    └─ FlightOptionDto.Segments: List<FlightSegmentDto>  (tương đương ListAvailFlt)
///     └─ ReturnOptions: List<FlightOptionDto>   (tương đương ListReturnFlight)
/// </summary>
public sealed class FareDataDto
{
    public string        FareId       { get; set; } = default!;
    public FlightSource  Source       { get; set; }
    public string        Airline      { get; set; } = default!;   // PlatingCarrier
    public string        Origin       { get; set; } = default!;
    public string        Destination  { get; set; } = default!;
    public DateTime      DepartDate   { get; set; }
    public DateTime?     ReturnDate   { get; set; }
    public TripType      TripType     { get; set; }

    // Pax counts
    public int AdultCount  { get; set; }
    public int ChildCount  { get; set; }
    public int InfantCount { get; set; }

    // Pricing per pax (raw fare from GDS, before commission/fee)
    public decimal AdultFare   { get; set; }
    public decimal ChildFare   { get; set; }
    public decimal InfantFare  { get; set; }
    public decimal BaseFareAdult   { get; set; }
    public decimal BaseFareChild   { get; set; }
    public decimal BaseFareInfant  { get; set; }
    public decimal TaxAdult    { get; set; }
    public decimal TaxChild    { get; set; }
    public decimal TaxInfant   { get; set; }

    // ── Backward-compatible alias ─────────────────────────────────────────────
    /// <summary>Total tax across all passenger types (backward-compatible alias).</summary>
    public decimal TaxAmount
    {
        get => TaxAdult + TaxChild + TaxInfant;
        set { /* ignored — use TaxAdult/TaxChild/TaxInfant instead */ }
    }

    public decimal ServiceFee  { get; set; }
    public decimal TotalFare   { get; set; }
    public string  Currency    { get; set; } = default!;

    // Engine session data (cần khi book)
    public string? SessionData { get; set; }
    public string? PccCode     { get; set; }

    // Nested flight options (matches old ListDepartureFlight / ListReturnFlight)
    public List<FlightOptionDto> OutboundOptions { get; set; } = [];
    public List<FlightOptionDto> ReturnOptions   { get; set; } = [];

    // ── Backward-compatible flat segment lists ───────────────────────────────
    // Flattens the first option's segments for code that expects the old flat structure.
    // New code should use OutboundOptions/ReturnOptions for full nested data.
    // These are settable for backward compatibility (e.g., BookOfflineCommand).
    public List<FlightSegmentDto> OutboundSegments { get; set; } = [];
    public List<FlightSegmentDto> ReturnSegments   { get; set; } = [];

    /// <summary>
    /// Populates OutboundSegments/ReturnSegments from the first option.
    /// Call this after setting OutboundOptions/ReturnOptions for backward compatibility.
    /// </summary>
    public void FlattenSegments()
    {
        OutboundSegments = OutboundOptions.FirstOrDefault()?.Segments ?? [];
        ReturnSegments   = ReturnOptions.FirstOrDefault()?.Segments ?? [];
    }

    // Fare rules XML (stored for GetFareRulesAsync)
    public List<string> DepartureRulesInfo { get; set; } = [];
    public List<string> ReturnRulesInfo    { get; set; } = [];

    public DateTime CachedAt  { get; set; }
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// Một option chuyến bay (tương đương Flight trong code cũ).
/// Mỗi option chứa nhiều segment (AvailFlt) tạo thành một hành trình hoàn chỉnh.
/// Ví dụ: SGN→HAN→NRT là 1 option với 2 segments.
/// </summary>
public sealed class FlightOptionDto
{
    public int OptionId { get; set; }

    // Summary fields (lấy từ segment đầu/cuối)
    public string   Airline     { get; set; } = default!;
    public string   Origin      { get; set; } = default!;
    public string   Destination { get; set; } = default!;
    public DateTime DepartDate  { get; set; }
    public DateTime ArriveDate  { get; set; }
    public int      Duration    { get; set; }       // Tổng thời gian bay (phút)
    public int      StopCount   { get; set; }       // Số điểm dừng = Segments.Count - 1
    public bool     NoRefund    { get; set; }       // Từ PenaltyRules

    // Chi tiết từng segment
    public List<FlightSegmentDto> Segments { get; set; } = [];
}

/// <summary>
/// Một segment/chặng bay (tương đương AvailFlt trong code cũ).
/// </summary>
public sealed class FlightSegmentDto
{
    public string   FlightNumber  { get; set; } = default!;
    public string   Airline       { get; set; } = default!;
    public string   Origin        { get; set; } = default!;
    public string   Destination   { get; set; } = default!;
    public DateTime DepartTime    { get; set; }
    public DateTime ArriveTime    { get; set; }

    // Cabin class per passenger type (khác nhau cho ADT/CNN/INF)
    public string   ClassAdult    { get; set; } = default!;
    public string   ClassChild    { get; set; } = "";
    public string   ClassInfant   { get; set; } = "";

    // ── Backward-compatible alias ─────────────────────────────────────────────
    /// <summary>Cabin class for adult passengers (backward-compatible alias for ClassAdult).</summary>
    public string CabinClass
    {
        get => ClassAdult;
        set => ClassAdult = value;
    }

    public string?  AircraftType  { get; set; }
    public int      StopCount     { get; set; }
    public int      Duration      { get; set; }       // Thời gian bay chặng này (phút)
    public int      StopTime      { get; set; }       // Thời gian chờ tại điểm dừng (phút)
    public string?  AirportChange { get; set; }       // AirpChg
    public string?  OperatingAirline { get; set; }    // OpAirV
    public string?  StartTerminal { get; set; }
    public string?  EndTerminal   { get; set; }
    public bool     IsLastSegment { get; set; }

    /// <summary>
    /// Engine-specific per-segment booking token.
    /// Used by Datacom (FlightValue), Maybay (SelectValue).
    /// </summary>
    public string?  SelectedValue { get; set; }
}

// ── Baggage DTOs ─────────────────────────────────────────────────────────────

public sealed class BaggageInfoDto
{
    public List<BaggageOptionDto> DepartBaggages  { get; set; } = [];
    public List<BaggageOptionDto> ReturnBaggages  { get; set; } = [];
}

public sealed class BaggageOptionDto
{
    public string  AirlineCode { get; set; } = default!;
    public string  Code        { get; set; } = default!;
    public string  Name        { get; set; } = default!;
    public string  Value       { get; set; } = default!;
    public decimal Price       { get; set; }
    public string  Currency    { get; set; } = default!;
}

// ── FareRule DTOs ─────────────────────────────────────────────────────────────

public sealed class FareRuleGroupDto
{
    public string             Title { get; set; } = default!;
    public List<string>       Rules { get; set; } = [];
}
