using Flight.Application.Dtos;
using Flight.Application.Features.Admin.Airlines;
using Flight.Domain.Aggregates.Booking;
using Flight.Domain.Repositories;
using MediatR;
using System.Linq;

namespace Flight.Application.Features.Admin.Aircrafts;

public sealed record GetAircraftsQuery(
    int Page = 1,
    int PageSize = 20,
    string? IATA = null)
    : IRequest<PaginatedResult<AircraftDto>>;

public sealed record GetAircraftByIdQuery(int Id) : IRequest<AircraftDto?>;

public sealed record CreateAircraftCommand(string IATA, string Manufacturer, string Model) : IRequest<AircraftDto>;

public sealed record UpdateAircraftCommand(int Id, string Manufacturer, string Model) : IRequest<AircraftDto?>;

public sealed record DeleteAircraftCommand(int Id) : IRequest<bool>;

public sealed class GetAircraftsHandler(IReferenceDataRepository repo) : IRequestHandler<GetAircraftsQuery, PaginatedResult<AircraftDto>>
{
    public async Task<PaginatedResult<AircraftDto>> Handle(GetAircraftsQuery request, CancellationToken ct)
    {
        var query = repo.GetAircraftsQuery();

        if (!string.IsNullOrEmpty(request.IATA))
            query = query.Where(a => a.IATA.Contains(request.IATA));

        var total = query.Count();
        var list = query
            .OrderBy(a => a.IATA)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new AircraftDto { Id = a.Id, IATA = a.IATA, Manufacturer = a.Manufacturer, Model = a.Model, Visible = a.Visible })
            .ToList();

        return new PaginatedResult<AircraftDto>(total, request.Page, request.PageSize, list);
    }
}

public sealed class GetAircraftByIdHandler(IReferenceDataRepository repo) : IRequestHandler<GetAircraftByIdQuery, AircraftDto?>
{
    public async Task<AircraftDto?> Handle(GetAircraftByIdQuery request, CancellationToken ct)
    {
        var entity = await repo.GetAircraftByIdAsync(request.Id, ct);
        return entity is null ? null : new AircraftDto { Id = entity.Id, IATA = entity.IATA, Manufacturer = entity.Manufacturer, Model = entity.Model, Visible = entity.Visible };
    }
}

public sealed class CreateAircraftHandler(IReferenceDataRepository repo) : IRequestHandler<CreateAircraftCommand, AircraftDto>
{
    public async Task<AircraftDto> Handle(CreateAircraftCommand request, CancellationToken ct)
    {
        var entity = AircraftEntity.Create(request.IATA, request.Manufacturer, request.Model);
        await repo.AddAircraftAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        return new AircraftDto { Id = entity.Id, IATA = entity.IATA, Manufacturer = entity.Manufacturer, Model = entity.Model, Visible = entity.Visible };
    }
}

public sealed class UpdateAircraftHandler(IReferenceDataRepository repo) : IRequestHandler<UpdateAircraftCommand, AircraftDto?>
{
    public async Task<AircraftDto?> Handle(UpdateAircraftCommand request, CancellationToken ct)
    {
        var entity = await repo.GetAircraftByIdAsync(request.Id, ct);
        if (entity is null) return null;

        entity.Manufacturer = request.Manufacturer;
        entity.Model = request.Model;
        await repo.SaveChangesAsync(ct);
        return new AircraftDto { Id = entity.Id, IATA = entity.IATA, Manufacturer = entity.Manufacturer, Model = entity.Model, Visible = entity.Visible };
    }
}

public sealed class DeleteAircraftHandler(IReferenceDataRepository repo) : IRequestHandler<DeleteAircraftCommand, bool>
{
    public async Task<bool> Handle(DeleteAircraftCommand request, CancellationToken ct)
    {
        var entity = await repo.GetAircraftByIdAsync(request.Id, ct);
        if (entity is null) return false;

        entity.Visible = false;
        await repo.SaveChangesAsync(ct);
        return true;
    }
}