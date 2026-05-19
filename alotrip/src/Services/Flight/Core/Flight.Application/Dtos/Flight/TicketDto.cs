namespace Flight.Application.Dtos;

public sealed class TicketDto
{
    public Guid BookingId { get; init; }
    public Guid PassengerId { get; init; }
    public string TicketNumber { get; init; } = default!;
    public string PassengerName { get; init; } = default!;
    public string Airline { get; init; } = default!;
    public DateTime IssuedAt { get; init; }
}
