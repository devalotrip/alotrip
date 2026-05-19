using Flight.Application.Dtos;
using Flight.Application.Features.Admin.Airlines;
using Flight.Domain.Aggregates.Booking;
using Flight.Domain.Repositories;
using MediatR;
using System.Linq;

namespace Flight.Application.Features.Admin.TripCancellations;

public sealed record GetTripCancellationsQuery(int Page = 1, int PageSize = 20, Guid? BookingId = null) : IRequest<PaginatedResult<TripCancellationDto>>;
public sealed record GetTripCancellationByIdQuery(int Id) : IRequest<TripCancellationDto?>;
public sealed record CreateTripCancellationCommand(Guid BookingId, decimal MarkupAmount, decimal MarkupPercent, decimal Price, string Currency, decimal BookingPrice, string? Value) : IRequest<TripCancellationDto>;
public sealed record DeleteTripCancellationCommand(int Id) : IRequest<bool>;

public sealed class GetTripCancellationsHandler(IBookingAddonRepository repo) : IRequestHandler<GetTripCancellationsQuery, PaginatedResult<TripCancellationDto>>
{
    public async Task<PaginatedResult<TripCancellationDto>> Handle(GetTripCancellationsQuery request, CancellationToken ct)
    {
        var query = repo.GetTripCancellationsQuery();
        if (request.BookingId.HasValue) query = query.Where(t => t.BookingId == request.BookingId);
        var total = query.Count();
        var list = query.OrderByDescending(t => t.CreatedOnUtc).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(t => new TripCancellationDto { Id = t.Id, BookingId = t.BookingId, MarkupAmount = t.MarkupAmount, MarkupPercent = t.MarkupPercent, Price = t.Price, Currency = t.Currency, BookingPrice = t.BookingPrice, Value = t.Value }).ToList();
        return new PaginatedResult<TripCancellationDto>(total, request.Page, request.PageSize, list);
    }
}

public sealed class GetTripCancellationByIdHandler(IBookingAddonRepository repo) : IRequestHandler<GetTripCancellationByIdQuery, TripCancellationDto?>
{
    public async Task<TripCancellationDto?> Handle(GetTripCancellationByIdQuery request, CancellationToken ct)
    {
        var entity = await repo.GetTripCancellationByIdAsync(request.Id, ct);
        return entity is null ? null : new TripCancellationDto { Id = entity.Id, BookingId = entity.BookingId, MarkupAmount = entity.MarkupAmount, MarkupPercent = entity.MarkupPercent, Price = entity.Price, Currency = entity.Currency, BookingPrice = entity.BookingPrice, Value = entity.Value };
    }
}

public sealed class CreateTripCancellationHandler(IBookingAddonRepository repo) : IRequestHandler<CreateTripCancellationCommand, TripCancellationDto>
{
    public async Task<TripCancellationDto> Handle(CreateTripCancellationCommand request, CancellationToken ct)
    {
        var entity = TripCancellationEntity.Create(request.BookingId, request.MarkupAmount, request.MarkupPercent, request.Price, request.Currency, request.BookingPrice, request.Value);
        await repo.AddTripCancellationAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        return new TripCancellationDto { Id = entity.Id, BookingId = entity.BookingId, MarkupAmount = entity.MarkupAmount, MarkupPercent = entity.MarkupPercent, Price = entity.Price, Currency = entity.Currency, BookingPrice = entity.BookingPrice, Value = entity.Value };
    }
}

public sealed class DeleteTripCancellationHandler(IBookingAddonRepository repo) : IRequestHandler<DeleteTripCancellationCommand, bool>
{
    public async Task<bool> Handle(DeleteTripCancellationCommand request, CancellationToken ct)
    {
        var entity = await repo.GetTripCancellationByIdAsync(request.Id, ct);
        if (entity is null) return false;
        entity.DeletedAt = DateTime.UtcNow;
        await repo.SaveChangesAsync(ct);
        return true;
    }
}