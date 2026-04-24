using Flight.Application.Dtos;
using Flight.Application.Features.Admin.Airlines;
using Flight.Domain.Aggregates.Booking;
using Flight.Domain.Repositories;
using MediatR;
using System.Linq;

namespace Flight.Application.Features.Admin.TripVisas;

public sealed record GetTripVisasQuery(int Page = 1, int PageSize = 20, Guid? BookingId = null) : IRequest<PaginatedResult<TripVisaDto>>;
public sealed record GetTripVisaByIdQuery(int Id) : IRequest<TripVisaDto?>;
public sealed record CreateTripVisaCommand(Guid BookingId, string Code, string Name, decimal? Price, string? Value, string Currency, decimal? PriceVn) : IRequest<TripVisaDto>;
public sealed record DeleteTripVisaCommand(int Id) : IRequest<bool>;

public sealed class GetTripVisasHandler(IBookingAddonRepository repo) : IRequestHandler<GetTripVisasQuery, PaginatedResult<TripVisaDto>>
{
    public async Task<PaginatedResult<TripVisaDto>> Handle(GetTripVisasQuery request, CancellationToken ct)
    {
        var query = repo.GetTripVisasQuery();
        if (request.BookingId.HasValue) query = query.Where(t => t.BookingId == request.BookingId);
        var total = query.Count();
        var list = query.OrderByDescending(t => t.CreatedOnUtc).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(t => new TripVisaDto { Id = t.Id, BookingId = t.BookingId, Code = t.Code, Name = t.Name, Price = t.Price, Value = t.Value, Currency = t.Currency, PriceVn = t.PriceVn }).ToList();
        return new PaginatedResult<TripVisaDto>(total, request.Page, request.PageSize, list);
    }
}

public sealed class GetTripVisaByIdHandler(IBookingAddonRepository repo) : IRequestHandler<GetTripVisaByIdQuery, TripVisaDto?>
{
    public async Task<TripVisaDto?> Handle(GetTripVisaByIdQuery request, CancellationToken ct)
    {
        var entity = await repo.GetTripVisaByIdAsync(request.Id, ct);
        return entity is null ? null : new TripVisaDto { Id = entity.Id, BookingId = entity.BookingId, Code = entity.Code, Name = entity.Name, Price = entity.Price, Value = entity.Value, Currency = entity.Currency, PriceVn = entity.PriceVn };
    }
}

public sealed class CreateTripVisaHandler(IBookingAddonRepository repo) : IRequestHandler<CreateTripVisaCommand, TripVisaDto>
{
    public async Task<TripVisaDto> Handle(CreateTripVisaCommand request, CancellationToken ct)
    {
        var entity = TripVisaEntity.Create(request.BookingId, request.Code, request.Name, request.Price, request.Value, request.Currency, request.PriceVn);
        await repo.AddTripVisaAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        return new TripVisaDto { Id = entity.Id, BookingId = entity.BookingId, Code = entity.Code, Name = entity.Name, Price = entity.Price, Value = entity.Value, Currency = entity.Currency, PriceVn = entity.PriceVn };
    }
}

public sealed class DeleteTripVisaHandler(IBookingAddonRepository repo) : IRequestHandler<DeleteTripVisaCommand, bool>
{
    public async Task<bool> Handle(DeleteTripVisaCommand request, CancellationToken ct)
    {
        var entity = await repo.GetTripVisaByIdAsync(request.Id, ct);
        if (entity is null) return false;
        entity.DeletedAt = DateTime.UtcNow;
        await repo.SaveChangesAsync(ct);
        return true;
    }
}