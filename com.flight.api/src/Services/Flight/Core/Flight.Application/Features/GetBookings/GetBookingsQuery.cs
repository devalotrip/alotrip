using Flight.Application.Dtos;
using Flight.Domain.Aggregates.Booking;
using Flight.Domain.Repositories;
using MediatR;

namespace Flight.Application.Features.GetBookings;

// ── Response DTO ─────────────────────────────────────────────────────────────

public sealed class BookingSummaryDto
{
    public Guid   Id          { get; init; }
    public string BookingCode { get; init; } = default!;
    public string AgentCode   { get; init; } = default!;
    public string Source      { get; init; } = default!;
    public string TripType    { get; init; } = default!;
    public string Status      { get; init; } = default!;
    public string Origin      { get; init; } = default!;
    public string Destination { get; init; } = default!;
    public DateTime DepartDate  { get; init; }
    public DateTime? ReturnDate { get; init; }
    public decimal TotalAmount  { get; init; }
    public string  Currency     { get; init; } = default!;
    public string  ContactName  { get; init; } = default!;
    public string  ContactEmail { get; init; } = default!;
    public DateTime? ExpiresAt  { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public int PassengerCount   { get; init; }
    public int TicketCount      { get; init; }
}

// ── Query ─────────────────────────────────────────────────────────────────────

/// <summary>
/// GET /api/bookings — lấy danh sách bookings của agent hiện tại (paginated).
/// </summary>
public sealed record GetBookingsQuery(
    string AgentCode,
    int    Page     = 1,
    int    PageSize = 20)
    : IRequest<IEnumerable<BookingSummaryDto>>;

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class GetBookingsHandler(IBookingRepository repo)
    : IRequestHandler<GetBookingsQuery, IEnumerable<BookingSummaryDto>>
{
    public async Task<IEnumerable<BookingSummaryDto>> Handle(
        GetBookingsQuery request, CancellationToken ct)
    {
        int page     = Math.Max(1, request.Page);
        int pageSize = Math.Clamp(request.PageSize, 1, 100);

        var bookings = await repo.GetByAgentAsync(request.AgentCode, page, pageSize, ct);
        return bookings.Select(Map);
    }

    private static BookingSummaryDto Map(BookingEntity b) => new()
    {
        Id            = b.Id,
        BookingCode   = b.BookingCode,
        AgentCode     = b.AgentCode,
        Source        = b.Source.ToString(),
        TripType      = b.TripType.ToString(),
        Status        = b.Status.ToString(),
        Origin        = b.Origin,
        Destination   = b.Destination,
        DepartDate    = b.DepartDate,
        ReturnDate    = b.ReturnDate,
        TotalAmount   = b.TotalAmount,
        Currency      = b.Currency,
        ContactName   = b.ContactName,
        ContactEmail  = b.ContactEmail,
        ExpiresAt     = b.ExpiresAt,
        CreatedOnUtc  = b.CreatedOnUtc,
        PassengerCount = b.Passengers.Count,
        TicketCount    = b.Tickets.Count
    };
}
