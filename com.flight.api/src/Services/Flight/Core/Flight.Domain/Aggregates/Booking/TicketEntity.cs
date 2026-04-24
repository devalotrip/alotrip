using Shared.BuildingBlocks.Abstractions;

namespace Flight.Domain.Aggregates.Booking;

/// <summary>
/// Thông tin vé điện tử đã phát hành. Map từ tblTicket cũ.
/// </summary>
public sealed class TicketEntity : Entity<Guid>
{
    public Guid    BookingId    { get; private set; }
    public Guid    PassengerId  { get; private set; }
    public string  TicketNumber { get; private set; } = default!;
    public string  PassengerName { get; private set; } = default!;
    public string  Airline      { get; private set; } = default!;
    public DateTime IssuedAt    { get; private set; }

    public BookingEntity? Booking { get; set; }

    private TicketEntity() { }

    public static TicketEntity Create(
        Guid bookingId, Guid passengerId,
        string ticketNumber, string passengerName,
        string airline)
    {
        return new TicketEntity
        {
            Id            = Guid.NewGuid(),
            BookingId     = bookingId,
            PassengerId   = passengerId,
            TicketNumber  = ticketNumber,
            PassengerName = passengerName,
            Airline       = airline,
            IssuedAt      = DateTime.UtcNow,
            CreatedOnUtc  = DateTime.UtcNow
        };
    }
}
