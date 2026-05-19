using Flight.Application.Dtos;
using Flight.Domain.Aggregates.Booking;
using Flight.Domain.Repositories;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Flight.Application.Features.GetTickets;

public sealed record GetTicketsByBookingIdQuery(Guid BookingId) : IRequest<IEnumerable<TicketDto>>;

public sealed class GetTicketsByBookingIdQueryHandler(
    IBookingRepository bookingRepository) : IRequestHandler<GetTicketsByBookingIdQuery, IEnumerable<TicketDto>>
{
    public async Task<IEnumerable<TicketDto>> Handle(GetTicketsByBookingIdQuery request, CancellationToken ct)
    {
        var booking = await bookingRepository.GetByIdAsync(request.BookingId, ct);
        if (booking?.Tickets == null) return new List<TicketDto>();

        var list = new List<TicketDto>();
        foreach (var t in booking.Tickets)
        {
            list.Add(new TicketDto
            {
                BookingId   = booking.Id,
                PassengerId = t.PassengerId,
                TicketNumber = t.TicketNumber,
                PassengerName = t.PassengerName,
                Airline = t.Airline,
                IssuedAt = t.IssuedAt
            });
        }
        return list;
    }
}
