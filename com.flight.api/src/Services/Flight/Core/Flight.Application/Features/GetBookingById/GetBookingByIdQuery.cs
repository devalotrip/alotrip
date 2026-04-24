using Flight.Domain.Aggregates.Booking;
using Flight.Domain.Repositories;
using MediatR;
using Shared.BuildingBlocks.Exceptions;

namespace Flight.Application.Features.GetBookingById;

// ── Response DTOs ─────────────────────────────────────────────────────────────

public sealed class BookingDetailDto
{
    public Guid   Id          { get; init; }
    public string BookingCode { get; init; } = default!;
    public string AgentCode   { get; init; } = default!;
    public string Source      { get; init; } = default!;
    public string TripType    { get; init; } = default!;
    public string Status      { get; init; } = default!;
    public string Origin      { get; init; } = default!;
    public string Destination { get; init; } = default!;
    public DateTime  DepartDate   { get; init; }
    public DateTime? ReturnDate   { get; init; }
    public decimal   TotalAmount  { get; init; }
    public decimal   ServiceFee   { get; init; }
    public string    Currency     { get; init; } = default!;
    public string    ContactName  { get; init; } = default!;
    public string    ContactEmail { get; init; } = default!;
    public string    ContactPhone { get; init; } = default!;
    public DateTime? ExpiresAt    { get; init; }
    public DateTime  CreatedOnUtc { get; init; }
    public string?   FareId       { get; init; }
    public string?   PccCode      { get; init; }

    public IReadOnlyList<BookingPassengerDto> Passengers { get; init; } = [];
    public IReadOnlyList<BookingFlightDto>    Flights    { get; init; } = [];
    public IReadOnlyList<BookingTicketDto>    Tickets    { get; init; } = [];
}

public sealed class BookingPassengerDto
{
    public string FirstName  { get; init; } = default!;
    public string LastName   { get; init; } = default!;
    public string Type       { get; init; } = default!;
    public string Gender     { get; init; } = default!;
    public string? BirthDate { get; init; }
    public string? PassportNo { get; init; }
}

public sealed class BookingFlightDto
{
    public string   Airline     { get; init; } = default!;
    public string   Origin      { get; init; } = default!;
    public string   Destination { get; init; } = default!;
    public IReadOnlyList<BookingSegmentDto> Segments { get; init; } = [];
}

public sealed class BookingSegmentDto
{
    public string   FlightNumber { get; init; } = default!;
    public string   Airline      { get; init; } = default!;
    public string   Origin       { get; init; } = default!;
    public string   Destination  { get; init; } = default!;
    public DateTime DepartTime   { get; init; }
    public DateTime ArriveTime   { get; init; }
    public string   CabinClass   { get; init; } = default!;
}

public sealed class BookingTicketDto
{
    public string   TicketNumber   { get; init; } = default!;
    public string   PassengerName  { get; init; } = default!;
    public DateTime IssuedOnUtc    { get; init; }
}

// ── Query ─────────────────────────────────────────────────────────────────────

/// <summary>
/// GET /api/bookings/{id} — lấy chi tiết booking theo ID.
/// </summary>
public sealed record GetBookingByIdQuery(Guid BookingId)
    : IRequest<BookingDetailDto>;

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class GetBookingByIdHandler(IBookingRepository repo)
    : IRequestHandler<GetBookingByIdQuery, BookingDetailDto>
{
    public async Task<BookingDetailDto> Handle(
        GetBookingByIdQuery request, CancellationToken ct)
    {
        var booking = await repo.GetByIdAsync(request.BookingId, ct)
            ?? throw new NotFoundException($"Booking '{request.BookingId}' not found.");

        return Map(booking);
    }

    internal static BookingDetailDto Map(BookingEntity b) => new()
    {
        Id           = b.Id,
        BookingCode  = b.BookingCode,
        AgentCode    = b.AgentCode,
        Source       = b.Source.ToString(),
        TripType     = b.TripType.ToString(),
        Status       = b.Status.ToString(),
        Origin       = b.Origin,
        Destination  = b.Destination,
        DepartDate   = b.DepartDate,
        ReturnDate   = b.ReturnDate,
        TotalAmount  = b.TotalAmount,
        ServiceFee   = b.ServiceFee,
        Currency     = b.Currency,
        ContactName  = b.ContactName,
        ContactEmail = b.ContactEmail,
        ContactPhone = b.ContactPhone,
        ExpiresAt    = b.ExpiresAt,
        CreatedOnUtc = b.CreatedOnUtc,
        FareId       = b.FareId,
        PccCode      = b.PccCode,
        Passengers   = b.Passengers.Select(p => new BookingPassengerDto
        {
            FirstName   = p.FirstName,
            LastName    = p.LastName,
            Type        = p.Type.ToString(),
            Gender      = p.Gender,
            BirthDate   = p.BirthDate?.ToString("yyyy-MM-dd"),
            PassportNo  = p.PassportNo
        }).ToList(),
        Flights = b.Flights.Select(f => new BookingFlightDto
        {
            Airline     = f.Airline,
            Origin      = f.Origin,
            Destination = f.Destination,
            Segments  = f.Segments.Select(s => new BookingSegmentDto
            {
                FlightNumber = s.FlightNumber,
                Airline      = s.Airline,
                Origin       = s.Origin,
                Destination  = s.Destination,
                DepartTime   = s.DepartTime,
                ArriveTime   = s.ArriveTime,
                CabinClass   = s.CabinClass
            }).ToList()
        }).ToList(),
        Tickets = b.Tickets.Select(t => new BookingTicketDto
        {
            TicketNumber  = t.TicketNumber,
            PassengerName = t.PassengerName,
            IssuedOnUtc   = t.IssuedAt
        }).ToList()
    };
}
