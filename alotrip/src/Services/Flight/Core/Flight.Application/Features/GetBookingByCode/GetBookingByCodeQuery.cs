using Flight.Application.Features.GetBookingById;
using Flight.Domain.Repositories;
using MediatR;
using Shared.BuildingBlocks.Exceptions;

namespace Flight.Application.Features.GetBookingByCode;

// ── Query ─────────────────────────────────────────────────────────────────────

/// <summary>
/// GET /api/bookings/by-code/{code} — lấy chi tiết booking theo booking code (PNR).
/// </summary>
public sealed record GetBookingByCodeQuery(string BookingCode)
    : IRequest<BookingDetailDto>;

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class GetBookingByCodeHandler(IBookingRepository repo)
    : IRequestHandler<GetBookingByCodeQuery, BookingDetailDto>
{
    public async Task<BookingDetailDto> Handle(
        GetBookingByCodeQuery request, CancellationToken ct)
    {
        var booking = await repo.GetByCodeAsync(request.BookingCode, ct)
            ?? throw new NotFoundException($"Booking with code '{request.BookingCode}' not found.");

        return GetBookingByIdHandler.Map(booking);
    }
}
