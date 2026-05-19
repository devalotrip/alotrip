using Flight.Domain.Aggregates.Booking.Events;
using Flight.Domain.Enums;
using Flight.Domain.Exceptions;
using Flight.Domain.ValueObjects;
using Shared.BuildingBlocks.Abstractions;

namespace Flight.Domain.Aggregates.Booking;

/// <summary>
/// Booking Aggregate Root.
/// Map từ tblBooking + tblBookingFlight + tblPassenger + tblTicket cũ.
/// </summary>
public sealed class BookingEntity : Aggregate<Guid>
{
    // ── Core booking info ────────────────────────────────────────────────────
    public string        BookingCode   { get; private set; } = default!;
    public string        AgentCode     { get; private set; } = default!;
    public FlightSource  Source        { get; private set; }
    public TripType      TripType      { get; private set; }
    public BookingStatus Status        { get; private set; }

    // ── Route ────────────────────────────────────────────────────────────────
    public string   Origin       { get; private set; } = default!;
    public string   Destination  { get; private set; } = default!;
    public DateTime DepartDate   { get; private set; }
    public DateTime? ReturnDate  { get; private set; }

    // ── Pricing ──────────────────────────────────────────────────────────────
    public decimal TotalAmount   { get; private set; }
    public string  Currency      { get; private set; } = default!;
    public decimal ServiceFee    { get; private set; }

    // ── Contact ──────────────────────────────────────────────────────────────
    public string ContactName    { get; private set; } = default!;
    public string ContactEmail   { get; private set; } = default!;
    public string ContactPhone   { get; private set; } = default!;

    // ── Session / Engine ─────────────────────────────────────────────────────
    public string? SessionId     { get; private set; }
    public string? PccCode       { get; private set; }
    public string? FareId        { get; private set; }

    // ── Expiry ───────────────────────────────────────────────────────────────
    public DateTime? ExpiresAt   { get; private set; }

    // ── Navigation ───────────────────────────────────────────────────────────
    private readonly List<BookingFlightEntity>   _flights    = [];
    private readonly List<PassengerEntity>       _passengers = [];
    private readonly List<TicketEntity>          _tickets    = [];

    public IReadOnlyList<BookingFlightEntity>   Flights    => _flights.AsReadOnly();
    public IReadOnlyList<PassengerEntity>       Passengers => _passengers.AsReadOnly();
    public IReadOnlyList<TicketEntity>          Tickets    => _tickets.AsReadOnly();

    private BookingEntity() { }

    // ── Factory ──────────────────────────────────────────────────────────────
    public static BookingEntity Create(
        string bookingCode, string agentCode,
        FlightSource source, TripType tripType,
        string origin, string destination,
        DateTime departDate, DateTime? returnDate,
        decimal totalAmount, string currency, decimal serviceFee,
        string contactName, string contactEmail, string contactPhone,
        string? sessionId = null, string? pccCode = null, string? fareId = null,
        DateTime? expiresAt = null)
    {
        var booking = new BookingEntity
        {
            Id           = Guid.NewGuid(),
            BookingCode  = bookingCode,
            AgentCode    = agentCode,
            Source       = source,
            TripType     = tripType,
            Status       = BookingStatus.Pending,
            Origin       = origin,
            Destination  = destination,
            DepartDate   = departDate,
            ReturnDate   = returnDate,
            TotalAmount  = totalAmount,
            Currency     = currency,
            ServiceFee   = serviceFee,
            ContactName  = contactName,
            ContactEmail = contactEmail,
            ContactPhone = contactPhone,
            SessionId    = sessionId,
            PccCode      = pccCode,
            FareId       = fareId,
            ExpiresAt    = expiresAt,
            CreatedOnUtc = DateTime.UtcNow
        };

        booking.RaiseDomainEvent(new BookingCreatedDomainEvent(
            booking.Id, bookingCode, agentCode,
            contactEmail, contactPhone,
            origin, destination,
            totalAmount, currency,
            DateTimeOffset.UtcNow));

        return booking;
    }

    // ── Behaviors ────────────────────────────────────────────────────────────
    public void AddFlight(BookingFlightEntity flight)
        => _flights.Add(flight);

    public void AddPassenger(PassengerEntity passenger)
        => _passengers.Add(passenger);

    public void Confirm()
    {
        if (Status != BookingStatus.Pending)
            throw new DomainException($"Cannot confirm booking in status '{Status}'.");
        Status = BookingStatus.Confirmed;
        LastModifiedOnUtc = DateTime.UtcNow;
    }

    public void IssueTicket(TicketEntity ticket)
    {
        if (Status == BookingStatus.Ticketed)
            throw new BookingAlreadyTicketedException(Id);
        if (Status != BookingStatus.Confirmed)
            throw new DomainException($"Cannot issue ticket for booking in status '{Status}'.");

        _tickets.Add(ticket);

        if (_tickets.Count >= _passengers.Count)
        {
            Status = BookingStatus.Ticketed;
            RaiseDomainEvent(new TicketIssuedDomainEvent(
                Id, BookingCode, ticket.TicketNumber,
                ticket.PassengerName, ContactEmail,
                DateTimeOffset.UtcNow));
        }

        LastModifiedOnUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Re-submits an existing booking that was Pending or Failed to the airline.
    /// Used by BookOffline flow (admin retry).
    /// </summary>
    public void ConfirmOffline(string bookingCode)
    {
        if (Status != BookingStatus.Pending && Status != BookingStatus.Failed)
            throw new DomainException($"Cannot confirm offline booking in status '{Status}'. Must be Pending or Failed.");

        BookingCode = bookingCode;
        Status = BookingStatus.Confirmed;
        LastModifiedOnUtc = DateTime.UtcNow;
    }

    public void MarkFailed()
    {
        if (Status == BookingStatus.Ticketed)
            throw new DomainException("Cannot mark a ticketed booking as failed.");

        Status = BookingStatus.Failed;
        LastModifiedOnUtc = DateTime.UtcNow;
    }

    public void Cancel(string reason)
    {
        if (Status == BookingStatus.Ticketed)
            throw new DomainException("Cannot cancel a ticketed booking. Contact support.");
        if (Status == BookingStatus.Cancelled)
            throw new DomainException("Booking is already cancelled.");

        Status = BookingStatus.Cancelled;
        RaiseDomainEvent(new BookingCancelledDomainEvent(
            Id, BookingCode, reason, DateTimeOffset.UtcNow));

        LastModifiedOnUtc = DateTime.UtcNow;
    }

    public bool IsExpired() => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
}
