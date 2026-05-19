using Flight.Application.Dtos;
using Flight.Domain.Repositories;
using MediatR;
using System.Linq;

namespace Flight.Application.Features.GetBaggagesByBooking;

public sealed record GetBaggagesByBookingQuery(Guid BookingId) : IRequest<BaggageInfoDto>;

public sealed class GetBaggagesByBookingHandler(IBookingRepository repo) : IRequestHandler<GetBaggagesByBookingQuery, BaggageInfoDto>
{
    public async Task<BaggageInfoDto> Handle(GetBaggagesByBookingQuery request, CancellationToken ct)
    {
        var baggages = await repo.GetBaggagesByBookingIdAsync(request.BookingId, ct);

        var departBaggages = baggages.Select(b => new BaggageOptionDto
        {
            AirlineCode = "",
            Code = b.BaggageCode ?? "",
            Name = b.BaggageType ?? (b.Weight.HasValue ? $"{b.Weight}kg" : ""),
            Value = b.Weight?.ToString() ?? "",
            Price = 0,
            Currency = "VND"
        }).ToList();

        var result = new BaggageInfoDto
        {
            DepartBaggages = departBaggages,
            ReturnBaggages = new System.Collections.Generic.List<BaggageOptionDto>()
        };

        return result;
    }
}
