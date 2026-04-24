using Flight.Application.Dtos;
using Flight.Domain.Aggregates.Geo;
using Flight.Domain.Repositories;
using MediatR;

namespace Flight.Application.Features.Admin.Geo;

// ── Queries ──────────────────────────────────────────────────────────────────
public sealed record GetCitiesQuery : IRequest<IEnumerable<GeoCityDto>>;
public sealed record GetCityByCodeQuery(string Code) : IRequest<GeoCityDto?>;

// ── Commands ─────────────────────────────────────────────────────────────────
public sealed record CreateCityCommand(GeoCityDto Dto, string UserName) : IRequest<GeoCityDto>;
public sealed record UpdateCityCommand(string Code, GeoCityDto Dto, string UserName) : IRequest<GeoCityDto?>;
public sealed record DeleteCityCommand(string Code, string UserName) : IRequest<bool>;

// ── Handlers ─────────────────────────────────────────────────────────────────
public sealed class GetCitiesHandler(IGeoCityRepository repo) : IRequestHandler<GetCitiesQuery, IEnumerable<GeoCityDto>>
{
    public async Task<IEnumerable<GeoCityDto>> Handle(GetCitiesQuery request, CancellationToken ct)
    {
        var list = await repo.GetAllAsync(ct);
        return list.Select(e => new GeoCityDto
        {
            Code = e.Id, CountryCode = e.CountryCode,
            NameVi = e.NameVi, NameEn = e.NameEn, NameFr = e.NameFr,
            Location = e.Location, SearchKeys = e.SearchKeys, Visible = e.Visible
        });
    }
}

public sealed class GetCityByCodeHandler(IGeoCityRepository repo) : IRequestHandler<GetCityByCodeQuery, GeoCityDto?>
{
    public async Task<GeoCityDto?> Handle(GetCityByCodeQuery request, CancellationToken ct)
    {
        var e = await repo.GetByCodeAsync(request.Code, ct);
        return e is null ? null : new GeoCityDto
        {
            Code = e.Id, CountryCode = e.CountryCode,
            NameVi = e.NameVi, NameEn = e.NameEn, NameFr = e.NameFr,
            Location = e.Location, SearchKeys = e.SearchKeys, Visible = e.Visible
        };
    }
}

public sealed class CreateCityHandler(IGeoCityRepository repo) : IRequestHandler<CreateCityCommand, GeoCityDto>
{
    public async Task<GeoCityDto> Handle(CreateCityCommand request, CancellationToken ct)
    {
        var dto = request.Dto;
        var entity = GeoCity.Create(dto.Code, dto.CountryCode, dto.NameVi, dto.NameEn, dto.NameFr, dto.Visible, dto.Location, dto.SearchKeys);
        entity.CreatedBy = request.UserName;
        await repo.AddAsync(entity, ct);
        return new GeoCityDto
        {
            Code = entity.Id, CountryCode = entity.CountryCode,
            NameVi = entity.NameVi, NameEn = entity.NameEn, NameFr = entity.NameFr,
            Location = entity.Location, SearchKeys = entity.SearchKeys, Visible = entity.Visible
        };
    }
}

public sealed class UpdateCityHandler(IGeoCityRepository repo) : IRequestHandler<UpdateCityCommand, GeoCityDto?>
{
    public async Task<GeoCityDto?> Handle(UpdateCityCommand request, CancellationToken ct)
    {
        var entity = await repo.GetByCodeAsync(request.Code, ct);
        if (entity is null) return null;

        entity.CountryCode = request.Dto.CountryCode;
        entity.NameVi = request.Dto.NameVi;
        entity.NameEn = request.Dto.NameEn;
        entity.NameFr = request.Dto.NameFr;
        entity.Location = request.Dto.Location;
        entity.SearchKeys = request.Dto.SearchKeys;
        entity.Visible = request.Dto.Visible;
        entity.LastModifiedBy = request.UserName;
        repo.Update(entity);
        return new GeoCityDto
        {
            Code = entity.Id, CountryCode = entity.CountryCode,
            NameVi = entity.NameVi, NameEn = entity.NameEn, NameFr = entity.NameFr,
            Location = entity.Location, SearchKeys = entity.SearchKeys, Visible = entity.Visible
        };
    }
}

public sealed class DeleteCityHandler(IGeoCityRepository repo) : IRequestHandler<DeleteCityCommand, bool>
{
    public async Task<bool> Handle(DeleteCityCommand request, CancellationToken ct)
    {
        var entity = await repo.GetByCodeAsync(request.Code, ct);
        if (entity is null) return false;

        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedBy = request.UserName;
        repo.Update(entity);
        return true;
    }
}
