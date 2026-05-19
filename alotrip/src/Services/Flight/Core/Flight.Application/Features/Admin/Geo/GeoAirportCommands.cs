using Flight.Application.Dtos;
using Flight.Domain.Aggregates.Geo;
using Flight.Domain.Repositories;
using MediatR;

namespace Flight.Application.Features.Admin.Geo;

// ── Queries ──────────────────────────────────────────────────────────────────
public sealed record GetAirportsQuery : IRequest<IEnumerable<GeoAirportDto>>;
public sealed record GetAirportByCodeQuery(string Code) : IRequest<GeoAirportDto?>;

// ── Commands ─────────────────────────────────────────────────────────────────
public sealed record CreateAirportCommand(GeoAirportDto Dto, string UserName) : IRequest<GeoAirportDto>;
public sealed record UpdateAirportCommand(string Code, GeoAirportDto Dto, string UserName) : IRequest<GeoAirportDto?>;
public sealed record DeleteAirportCommand(string Code, string UserName) : IRequest<bool>;

// ── Handlers ─────────────────────────────────────────────────────────────────
public sealed class GetAirportsHandler(IGeoAirportRepository repo) : IRequestHandler<GetAirportsQuery, IEnumerable<GeoAirportDto>>
{
    public async Task<IEnumerable<GeoAirportDto>> Handle(GetAirportsQuery request, CancellationToken ct)
    {
        var list = await repo.GetAllAsync(ct);
        return list.Select(e => new GeoAirportDto
        {
            Code = e.Id, CityCode = e.CityCode,
            NameVi = e.NameVi, NameEn = e.NameEn, NameFr = e.NameFr,
            Location = e.Location, SearchKeys = e.SearchKeys, Visible = e.Visible
        });
    }
}

public sealed class GetAirportByCodeHandler(IGeoAirportRepository repo) : IRequestHandler<GetAirportByCodeQuery, GeoAirportDto?>
{
    public async Task<GeoAirportDto?> Handle(GetAirportByCodeQuery request, CancellationToken ct)
    {
        var e = await repo.GetByCodeAsync(request.Code, ct);
        return e is null ? null : new GeoAirportDto
        {
            Code = e.Id, CityCode = e.CityCode,
            NameVi = e.NameVi, NameEn = e.NameEn, NameFr = e.NameFr,
            Location = e.Location, SearchKeys = e.SearchKeys, Visible = e.Visible
        };
    }
}

public sealed class CreateAirportHandler(IGeoAirportRepository repo) : IRequestHandler<CreateAirportCommand, GeoAirportDto>
{
    public async Task<GeoAirportDto> Handle(CreateAirportCommand request, CancellationToken ct)
    {
        var dto = request.Dto;
        var entity = GeoAirport.Create(dto.Code, dto.CityCode, dto.NameVi, dto.NameEn, dto.NameFr, dto.Visible, dto.Location, dto.SearchKeys);
        entity.CreatedBy = request.UserName;
        await repo.AddAsync(entity, ct);
        return new GeoAirportDto
        {
            Code = entity.Id, CityCode = entity.CityCode,
            NameVi = entity.NameVi, NameEn = entity.NameEn, NameFr = entity.NameFr,
            Location = entity.Location, SearchKeys = entity.SearchKeys, Visible = entity.Visible
        };
    }
}

public sealed class UpdateAirportHandler(IGeoAirportRepository repo) : IRequestHandler<UpdateAirportCommand, GeoAirportDto?>
{
    public async Task<GeoAirportDto?> Handle(UpdateAirportCommand request, CancellationToken ct)
    {
        var entity = await repo.GetByCodeAsync(request.Code, ct);
        if (entity is null) return null;

        entity.CityCode = request.Dto.CityCode;
        entity.NameVi = request.Dto.NameVi;
        entity.NameEn = request.Dto.NameEn;
        entity.NameFr = request.Dto.NameFr;
        entity.Location = request.Dto.Location;
        entity.SearchKeys = request.Dto.SearchKeys;
        entity.Visible = request.Dto.Visible;
        entity.LastModifiedBy = request.UserName;
        repo.Update(entity);
        return new GeoAirportDto
        {
            Code = entity.Id, CityCode = entity.CityCode,
            NameVi = entity.NameVi, NameEn = entity.NameEn, NameFr = entity.NameFr,
            Location = entity.Location, SearchKeys = entity.SearchKeys, Visible = entity.Visible
        };
    }
}

public sealed class DeleteAirportHandler(IGeoAirportRepository repo) : IRequestHandler<DeleteAirportCommand, bool>
{
    public async Task<bool> Handle(DeleteAirportCommand request, CancellationToken ct)
    {
        var entity = await repo.GetByCodeAsync(request.Code, ct);
        if (entity is null) return false;

        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedBy = request.UserName;
        repo.Update(entity);
        return true;
    }
}
