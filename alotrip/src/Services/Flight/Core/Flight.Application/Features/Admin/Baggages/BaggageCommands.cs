using Flight.Application.Dtos;
using Flight.Application.Features.Admin.Airlines;
using Flight.Domain.Aggregates.Booking;
using Flight.Domain.Repositories;
using MediatR;
using System.Linq;

namespace Flight.Application.Features.Admin.Baggages;

public sealed record GetBaggagesQuery(int Page = 1, int PageSize = 20, Guid? BookingId = null, string? FlightNumber = null) : IRequest<PaginatedResult<BaggageDto>>;
public sealed record GetBaggageByIdQuery(int Id) : IRequest<BaggageDto?>;
public sealed record CreateBaggageCommand(string? BaggageCode, int? FlightId, string? FlightNumber, int? PaxId, Guid? BookingId, int? Weight, int? WeightUnit, int? PieceAllowance, string? CabinClass, string? BaggageType) : IRequest<BaggageDto>;
public sealed record DeleteBaggageCommand(int Id) : IRequest<bool>;

public sealed class GetBaggagesHandler(IReferenceDataRepository repo) : IRequestHandler<GetBaggagesQuery, PaginatedResult<BaggageDto>>
{
    public async Task<PaginatedResult<BaggageDto>> Handle(GetBaggagesQuery request, CancellationToken ct)
    {
        var query = repo.GetBaggagesQuery();
        if (request.BookingId.HasValue) query = query.Where(b => b.BookingId == request.BookingId);
        if (!string.IsNullOrEmpty(request.FlightNumber)) query = query.Where(b => b.FlightNumber == request.FlightNumber);
        var total = query.Count();
        var list = query.OrderByDescending(b => b.CreatedOnUtc).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(b => new BaggageDto { Id = b.Id, BaggageCode = b.BaggageCode, FlightId = b.FlightId, FlightNumber = b.FlightNumber, PaxId = b.PaxId, BookingId = b.BookingId, Weight = b.Weight, WeightUnit = b.WeightUnit, PieceAllowance = b.PieceAllowance, CabinClass = b.CabinClass, BaggageType = b.BaggageType }).ToList();
        return new PaginatedResult<BaggageDto>(total, request.Page, request.PageSize, list);
    }
}

public sealed class GetBaggageByIdHandler(IReferenceDataRepository repo) : IRequestHandler<GetBaggageByIdQuery, BaggageDto?>
{
    public async Task<BaggageDto?> Handle(GetBaggageByIdQuery request, CancellationToken ct)
    {
        var entity = await repo.GetBaggageByIdAsync(request.Id, ct);
        return entity is null ? null : new BaggageDto { Id = entity.Id, BaggageCode = entity.BaggageCode, FlightId = entity.FlightId, FlightNumber = entity.FlightNumber, PaxId = entity.PaxId, BookingId = entity.BookingId, Weight = entity.Weight, WeightUnit = entity.WeightUnit, PieceAllowance = entity.PieceAllowance, CabinClass = entity.CabinClass, BaggageType = entity.BaggageType };
    }
}

public sealed class CreateBaggageHandler(IReferenceDataRepository repo) : IRequestHandler<CreateBaggageCommand, BaggageDto>
{
    public async Task<BaggageDto> Handle(CreateBaggageCommand request, CancellationToken ct)
    {
        var entity = BaggageEntity.Create(request.BaggageCode, request.FlightId, request.FlightNumber, request.PaxId, request.BookingId, request.Weight, request.WeightUnit, request.PieceAllowance, request.CabinClass, request.BaggageType);
        await repo.AddBaggageAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        return new BaggageDto { Id = entity.Id, BaggageCode = entity.BaggageCode, FlightId = entity.FlightId, FlightNumber = entity.FlightNumber, PaxId = entity.PaxId, BookingId = entity.BookingId, Weight = entity.Weight, WeightUnit = entity.WeightUnit, PieceAllowance = entity.PieceAllowance, CabinClass = entity.CabinClass, BaggageType = entity.BaggageType };
    }
}

public sealed class DeleteBaggageHandler(IReferenceDataRepository repo) : IRequestHandler<DeleteBaggageCommand, bool>
{
    public async Task<bool> Handle(DeleteBaggageCommand request, CancellationToken ct)
    {
        var entity = await repo.GetBaggageByIdAsync(request.Id, ct);
        if (entity is null) return false;
        entity.DeletedAt = DateTime.UtcNow;
        await repo.SaveChangesAsync(ct);
        return true;
    }
}