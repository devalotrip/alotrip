using Flight.Domain.Aggregates.Booking;
using Flight.Domain.Enums;

namespace Flight.Domain.Repositories;

/// <summary>
/// Core booking aggregate repository — booking lifecycle operations only.
/// Other domain concerns split into ISP-compliant interfaces:
///   IAgentRepository, IReferenceDataRepository, IBookingAddonRepository,
///   IUserRepository, ISearchAnalyticRepository, IAdminBookingRepository.
/// </summary>
public interface IBookingRepository
{
    Task SaveChangesAsync(CancellationToken ct = default);

    Task<BookingEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Loads booking with all navigation: Flights (with Segments), Passengers.
    /// Needed by BookOffline which reconstructs FareData from stored entities.
    /// </summary>
    Task<BookingEntity?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);

    Task<BookingEntity?> GetByCodeAsync(string bookingCode, CancellationToken ct = default);
    Task<IEnumerable<BookingEntity>> GetByAgentAsync(string agentCode, int page, int pageSize, CancellationToken ct = default);

    /// <summary>
    /// Returns true if there is already a Pending or Confirmed booking for the given
    /// fare + agent combination, preventing accidental double-booking.
    /// </summary>
    Task<bool> ExistsActiveByFareIdAsync(string fareId, string agentCode, CancellationToken ct = default);

    /// <summary>
    /// Finds recent Pending/Confirmed bookings matching route/date/agent within a time window.
    /// Used for deep duplicate detection — the caller compares segments, passengers, and contact info.
    /// Eagerly loads Flights (with Segments) and Passengers.
    /// </summary>
    Task<List<BookingEntity>> GetCandidateDuplicateBookingsAsync(
        string origin, string destination, DateTime departDate, DateTime? returnDate,
        int passengerCount, string agentCode, double duplicateWindowMinutes,
        CancellationToken ct = default);

    // Ticket
    Task<TicketEntity?> GetTicketByNumberAsync(string ticketNumber, CancellationToken ct = default);

    // Baggage (booking-scoped query)
    Task<List<BaggageEntity>> GetBaggagesByBookingIdAsync(Guid bookingId, CancellationToken ct = default);

    Task AddAsync(BookingEntity booking, CancellationToken ct = default);
    void Update(BookingEntity booking);
}

// ── DTOs used by repository contracts ─────────────────────────────────────────
// These remain here so that all repository interfaces in the Domain layer can reference them.

public sealed class AgentDto
{
    public int Id { get; init; }
    public string AgentCode { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string Email { get; init; } = default!;
    public string? Tel { get; init; }
    public bool Active { get; init; }
    public string? DefaultCurrency { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime ExpiryDate { get; init; }
}

public sealed class AgentDetailDto
{
    public int Id { get; init; }
    public string AgentCode { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string Email { get; init; } = default!;
    public string? Tel { get; init; }
    public string? Address { get; init; }
    public bool Active { get; init; }
    public string? GalileoPcc { get; init; }
    public bool GalileoActive { get; init; }
    public bool LccVnActiveDomestic { get; init; }
    public bool LccVnActiveGlobal { get; init; }
    public bool EnableCache { get; init; }
    public int CacheTimeMinutes { get; init; }
    public string? DefaultCurrency { get; init; }
    public decimal BaggageFeePercent { get; init; }
    public decimal BaggageFeeAmount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime ExpiryDate { get; init; }
}

public sealed class UpsertCommissionRequest
{
    public int AgentId { get; set; }
    public string AirlineGroup { get; set; } = default!;
    public string StartRegion { get; set; } = default!;
    public string EndRegion { get; set; } = default!;
    public string Currency { get; set; } = "VND";
    public decimal FeeAdtOneWay { get; set; }
    public decimal FeeChdOneWay { get; set; }
    public decimal FeeInfOneWay { get; set; }
    public decimal FeeAdtRoundTrip { get; set; }
    public decimal FeeChdRoundTrip { get; set; }
    public decimal FeeInfRoundTrip { get; set; }
    public decimal FeeByPercent { get; set; }
    public decimal Commission { get; set; }
    public bool ComByPercent { get; set; }
    public bool ComPercentOnBaseFare { get; set; }
}

public sealed class CommissionDto
{
    public int Id { get; init; }
    public int AgentId { get; init; }
    public string AgentCode { get; init; } = default!;
    public string AirlineGroup { get; init; } = default!;
    public string StartRegion { get; init; } = default!;
    public string EndRegion { get; init; } = default!;
    public string Currency { get; init; } = default!;
    public decimal FeeAdtOneWay { get; init; }
    public decimal FeeChdOneWay { get; init; }
    public decimal FeeInfOneWay { get; init; }
    public decimal FeeAdtRoundTrip { get; init; }
    public decimal FeeChdRoundTrip { get; init; }
    public decimal FeeInfRoundTrip { get; init; }
    public decimal FeeByPercent { get; init; }
    public decimal Commission { get; init; }
    public bool ComByPercent { get; init; }
    public bool ComPercentOnBaseFare { get; init; }
}

public sealed class AdminBookingListDto
{
    public Guid Id { get; init; }
    public string BookingCode { get; init; } = default!;
    public string AgentCode { get; init; } = default!;
    public string Source { get; init; } = default!;
    public string TripType { get; init; } = default!;
    public string Status { get; init; } = default!;
    public string Origin { get; init; } = default!;
    public string Destination { get; init; } = default!;
    public DateTime DepartDate { get; init; }
    public DateTime? ReturnDate { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = default!;
    public string ContactName { get; init; } = default!;
    public string ContactEmail { get; init; } = default!;
    public DateTime ExpiresAt { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public int PassengerCount { get; init; }
    public int TicketCount { get; init; }
}

public sealed class AdminBookingDetailDto
{
    public Guid Id { get; init; }
    public string BookingCode { get; init; } = default!;
    public string AgentCode { get; init; } = default!;
    public string Source { get; init; } = default!;
    public string TripType { get; init; } = default!;
    public string Status { get; init; } = default!;
    public string Origin { get; init; } = default!;
    public string Destination { get; init; } = default!;
    public DateTime DepartDate { get; init; }
    public DateTime? ReturnDate { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal ServiceFee { get; init; }
    public string Currency { get; init; } = default!;
    public string ContactName { get; init; } = default!;
    public string ContactEmail { get; init; } = default!;
    public string? ContactPhone { get; init; }
    public DateTime ExpiresAt { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public string? FareId { get; init; }
    public string? PccCode { get; init; }
    public List<AdminPassengerDto> Passengers { get; init; } = [];
    public List<AdminFlightDto> Flights { get; init; } = [];
    public List<AdminTicketDto> Tickets { get; init; } = [];
}

public sealed class AdminPassengerDto
{
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public string Gender { get; init; } = default!;
    public string Type { get; init; } = default!;
    public string? BirthDate { get; init; }
    public string? PassportNo { get; init; }
    public decimal? BaggageKg { get; init; }
    public decimal FareAmount { get; init; }
}

public sealed class AdminFlightDto
{
    public string Airline { get; init; } = default!;
    public string Origin { get; init; } = default!;
    public string Destination { get; init; } = default!;
    public DateTime DepartTime { get; init; }
    public DateTime ArriveTime { get; init; }
    public List<AdminSegmentDto> Segments { get; init; } = [];
}

public sealed class AdminSegmentDto
{
    public string FlightNumber { get; init; } = default!;
    public string Airline { get; init; } = default!;
    public string Origin { get; init; } = default!;
    public string Destination { get; init; } = default!;
    public DateTime DepartTime { get; init; }
    public DateTime ArriveTime { get; init; }
    public string CabinClass { get; init; } = default!;
    public string AircraftType { get; init; } = default!;
}

public sealed class AdminTicketDto
{
    public string TicketNumber { get; init; } = default!;
    public string PassengerName { get; init; } = default!;
    public string Airline { get; init; } = default!;
    public DateTime IssuedAt { get; init; }
}

// Analytics DTOs
public sealed class BookingsSummaryDto
{
    public int TotalBookings { get; init; }
    public int ConfirmedBookings { get; init; }
    public int PendingBookings { get; init; }
    public int CancelledBookings { get; init; }
    public int ExpiredBookings { get; init; }
    public decimal TotalRevenue { get; init; }
    public string Currency { get; init; } = "VND";
}

public sealed class TicketsIssuedDto
{
    public int TotalTickets { get; init; }
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }
}

public sealed class SearchDetailsDto
{
    public int TotalSearches { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public List<SearchDetailItemDto> Searches { get; init; } = [];
}

public sealed class SearchDetailItemDto
{
    public string AgentCode { get; init; } = default!;
    public string Origin { get; init; } = default!;
    public string Destination { get; init; } = default!;
    public DateTime DepartDate { get; init; }
    public int AdultCount { get; init; }
    public int ChildCount { get; init; }
    public int InfantCount { get; init; }
    public DateTime SearchedAt { get; init; }
}
