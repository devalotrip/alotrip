using Flight.Application.Dtos;
using Flight.Application.Features.Admin.Airlines;
using Flight.Domain.Aggregates.Booking;
using Flight.Domain.Repositories;
using MediatR;
using System.Linq;

namespace Flight.Application.Features.Admin.CarRentals;

public sealed record GetCarRentalsQuery(int Page = 1, int PageSize = 20, Guid? BookingId = null) : IRequest<PaginatedResult<CarRentalDto>>;
public sealed record GetCarRentalByIdQuery(int Id) : IRequest<CarRentalDto?>;
public sealed record CreateCarRentalCommand(Guid BookingId, string Provider, string PickupLocation, string DropoffLocation, DateTime PickupDate, DateTime DropoffDate, string CarType, string CarModel, decimal DailyRate, string Currency) : IRequest<CarRentalDto>;
public sealed record DeleteCarRentalCommand(int Id) : IRequest<bool>;

public sealed class GetCarRentalsHandler(IBookingAddonRepository repo) : IRequestHandler<GetCarRentalsQuery, PaginatedResult<CarRentalDto>>
{
    public async Task<PaginatedResult<CarRentalDto>> Handle(GetCarRentalsQuery request, CancellationToken ct)
    {
        var query = repo.GetCarRentalsQuery();
        if (request.BookingId.HasValue) query = query.Where(c => c.BookingId == request.BookingId);
        var total = query.Count();
        var list = query.OrderByDescending(c => c.CreatedOnUtc).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(c => new CarRentalDto { Id = c.Id, BookingId = c.BookingId, Provider = c.Provider, PickupLocation = c.PickupLocation, DropoffLocation = c.DropoffLocation, PickupDate = c.PickupDate, DropoffDate = c.DropoffDate, CarType = c.CarType, CarModel = c.CarModel, DailyRate = c.DailyRate, TotalDays = c.TotalDays, TotalAmount = c.TotalAmount, Currency = c.Currency, Status = c.Status }).ToList();
        return new PaginatedResult<CarRentalDto>(total, request.Page, request.PageSize, list);
    }
}

public sealed class GetCarRentalByIdHandler(IBookingAddonRepository repo) : IRequestHandler<GetCarRentalByIdQuery, CarRentalDto?>
{
    public async Task<CarRentalDto?> Handle(GetCarRentalByIdQuery request, CancellationToken ct)
    {
        var entity = await repo.GetCarRentalByIdAsync(request.Id, ct);
        return entity is null ? null : new CarRentalDto { Id = entity.Id, BookingId = entity.BookingId, Provider = entity.Provider, PickupLocation = entity.PickupLocation, DropoffLocation = entity.DropoffLocation, PickupDate = entity.PickupDate, DropoffDate = entity.DropoffDate, CarType = entity.CarType, CarModel = entity.CarModel, DailyRate = entity.DailyRate, TotalDays = entity.TotalDays, TotalAmount = entity.TotalAmount, Currency = entity.Currency, Status = entity.Status };
    }
}

public sealed class CreateCarRentalHandler(IBookingAddonRepository repo) : IRequestHandler<CreateCarRentalCommand, CarRentalDto>
{
    public async Task<CarRentalDto> Handle(CreateCarRentalCommand request, CancellationToken ct)
    {
        var totalDays = (request.DropoffDate - request.PickupDate).Days;
        var totalAmount = request.DailyRate * totalDays;
        var entity = CarRentalEntity.Create(request.BookingId, request.Provider, request.PickupLocation, request.DropoffLocation, request.PickupDate, request.DropoffDate, request.CarType, request.CarModel, request.DailyRate, totalDays, totalAmount, request.Currency);
        await repo.AddCarRentalAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        return new CarRentalDto { Id = entity.Id, BookingId = entity.BookingId, Provider = entity.Provider, PickupLocation = entity.PickupLocation, DropoffLocation = entity.DropoffLocation, PickupDate = entity.PickupDate, DropoffDate = entity.DropoffDate, CarType = entity.CarType, CarModel = entity.CarModel, DailyRate = entity.DailyRate, TotalDays = entity.TotalDays, TotalAmount = entity.TotalAmount, Currency = entity.Currency, Status = entity.Status };
    }
}

public sealed class DeleteCarRentalHandler(IBookingAddonRepository repo) : IRequestHandler<DeleteCarRentalCommand, bool>
{
    public async Task<bool> Handle(DeleteCarRentalCommand request, CancellationToken ct)
    {
        var entity = await repo.GetCarRentalByIdAsync(request.Id, ct);
        if (entity is null) return false;
        entity.DeletedAt = DateTime.UtcNow;
        await repo.SaveChangesAsync(ct);
        return true;
    }
}