using Flight.Application.Dtos;
using Flight.Application.Features.Admin.Airlines;
using Flight.Domain.Aggregates.Booking;
using Flight.Domain.Repositories;
using MediatR;
using System.Linq;

namespace Flight.Application.Features.Admin.TripTours;

public sealed record GetTripToursQuery(int Page = 1, int PageSize = 20, Guid? BookingId = null) : IRequest<PaginatedResult<TripTourDto>>;
public sealed record GetTripTourByIdQuery(int Id) : IRequest<TripTourDto?>;
public sealed record CreateTripTourCommand(Guid BookingId, string TourName, string TourCode, DateTime DepartureDate, DateTime ReturnDate, string Destination, int PaxCount, decimal UnitPrice, decimal TotalAmount, string Currency, string Status) : IRequest<TripTourDto>;
public sealed record UpdateTripTourCommand(int Id, string TourName, string TourCode, DateTime DepartureDate, DateTime ReturnDate, string Destination, int PaxCount, decimal UnitPrice, decimal TotalAmount, string Currency, string Status) : IRequest<TripTourDto?>;
public sealed record DeleteTripTourCommand(int Id) : IRequest<bool>;

public sealed class GetTripToursHandler(IBookingAddonRepository repo) : IRequestHandler<GetTripToursQuery, PaginatedResult<TripTourDto>>
{
    public async Task<PaginatedResult<TripTourDto>> Handle(GetTripToursQuery request, CancellationToken ct)
    {
        var query = repo.GetTripToursQuery();
        if (request.BookingId.HasValue) query = query.Where(t => t.BookingId == request.BookingId);
        var total = query.Count();
        var list = query.OrderByDescending(t => t.DepartureDate).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(t => new TripTourDto { Id = t.Id, BookingId = t.BookingId, TourName = t.TourName, TourCode = t.TourCode, DepartureDate = t.DepartureDate, ReturnDate = t.ReturnDate, Destination = t.Destination, PaxCount = t.PaxCount, UnitPrice = t.UnitPrice, TotalAmount = t.TotalAmount, Currency = t.Currency, Status = t.Status }).ToList();
        return new PaginatedResult<TripTourDto>(total, request.Page, request.PageSize, list);
    }
}

public sealed class GetTripTourByIdHandler(IBookingAddonRepository repo) : IRequestHandler<GetTripTourByIdQuery, TripTourDto?>
{
    public async Task<TripTourDto?> Handle(GetTripTourByIdQuery request, CancellationToken ct)
    {
        var entity = await repo.GetTripTourByIdAsync(request.Id, ct);
        return entity is null ? null : new TripTourDto { Id = entity.Id, BookingId = entity.BookingId, TourName = entity.TourName, TourCode = entity.TourCode, DepartureDate = entity.DepartureDate, ReturnDate = entity.ReturnDate, Destination = entity.Destination, PaxCount = entity.PaxCount, UnitPrice = entity.UnitPrice, TotalAmount = entity.TotalAmount, Currency = entity.Currency, Status = entity.Status };
    }
}

public sealed class CreateTripTourHandler(IBookingAddonRepository repo) : IRequestHandler<CreateTripTourCommand, TripTourDto>
{
    public async Task<TripTourDto> Handle(CreateTripTourCommand request, CancellationToken ct)
    {
        var entity = TripTourEntity.Create(request.BookingId, request.TourName, request.TourCode, request.DepartureDate, request.ReturnDate, request.Destination, request.PaxCount, request.UnitPrice, request.TotalAmount, request.Currency);
        entity.Status = request.Status;
        await repo.AddTripTourAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        return new TripTourDto { Id = entity.Id, BookingId = entity.BookingId, TourName = entity.TourName, TourCode = entity.TourCode, DepartureDate = entity.DepartureDate, ReturnDate = entity.ReturnDate, Destination = entity.Destination, PaxCount = entity.PaxCount, UnitPrice = entity.UnitPrice, TotalAmount = entity.TotalAmount, Currency = entity.Currency, Status = entity.Status };
    }
}

public sealed class UpdateTripTourHandler(IBookingAddonRepository repo) : IRequestHandler<UpdateTripTourCommand, TripTourDto?>
{
    public async Task<TripTourDto?> Handle(UpdateTripTourCommand request, CancellationToken ct)
    {
        var entity = await repo.GetTripTourByIdAsync(request.Id, ct);
        if (entity is null) return null;
        entity.TourName = request.TourName;
        entity.TourCode = request.TourCode;
        entity.DepartureDate = request.DepartureDate;
        entity.ReturnDate = request.ReturnDate;
        entity.Destination = request.Destination;
        entity.PaxCount = request.PaxCount;
        entity.UnitPrice = request.UnitPrice;
        entity.TotalAmount = request.TotalAmount;
        entity.Currency = request.Currency;
        entity.Status = request.Status;
        await repo.SaveChangesAsync(ct);
        return new TripTourDto { Id = entity.Id, BookingId = entity.BookingId, TourName = entity.TourName, TourCode = entity.TourCode, DepartureDate = entity.DepartureDate, ReturnDate = entity.ReturnDate, Destination = entity.Destination, PaxCount = entity.PaxCount, UnitPrice = entity.UnitPrice, TotalAmount = entity.TotalAmount, Currency = entity.Currency, Status = entity.Status };
    }
}

public sealed class DeleteTripTourHandler(IBookingAddonRepository repo) : IRequestHandler<DeleteTripTourCommand, bool>
{
    public async Task<bool> Handle(DeleteTripTourCommand request, CancellationToken ct)
    {
        var entity = await repo.GetTripTourByIdAsync(request.Id, ct);
        if (entity is null) return false;
        entity.DeletedAt = DateTime.UtcNow;
        await repo.SaveChangesAsync(ct);
        return true;
    }
}