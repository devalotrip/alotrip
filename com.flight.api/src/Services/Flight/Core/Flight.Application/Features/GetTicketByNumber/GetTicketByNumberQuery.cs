using Flight.Application.Dtos;
using Flight.Domain.Repositories;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Flight.Application.Features.GetTicketByNumber;

public sealed record GetTicketByNumberQuery(string TicketNumber) : IRequest<TicketDto?>;

public sealed class GetTicketByNumberQueryHandler(IBookingRepository bookingRepository) : IRequestHandler<GetTicketByNumberQuery, TicketDto?>
{
    public async Task<TicketDto?> Handle(GetTicketByNumberQuery request, CancellationToken ct)
    {
        // Basic approach: search across bookings for a matching ticket
        // This requires BookingRepository to expose a method (GetTicketByNumberAsync).
        var ticket = await bookingRepository.GetTicketByNumberAsync(request.TicketNumber, ct);
        if (ticket == null) return null;
        return new TicketDto
        {
            BookingId = ticket.BookingId,
            PassengerId = ticket.PassengerId,
            TicketNumber = ticket.TicketNumber,
            PassengerName = ticket.PassengerName,
            Airline = ticket.Airline,
            IssuedAt = ticket.IssuedAt
        };
    }
}
