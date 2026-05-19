using Flight.Application.Dtos;
using Flight.Domain.Aggregates.Geo;
using Flight.Domain.Repositories;
using MediatR;

namespace Flight.Application.Features.Admin.Geo;

// ── Queries ──────────────────────────────────────────────────────────────────
public sealed record GetCountriesQuery : IRequest<IEnumerable<GeoCountryDto>>;
public sealed record GetCountryByCodeQuery(string Code) : IRequest<GeoCountryDto?>;

// ── Commands ─────────────────────────────────────────────────────────────────
public sealed record CreateCountryCommand(GeoCountryDto Dto, string UserName) : IRequest<GeoCountryDto>;
public sealed record UpdateCountryCommand(string Code, GeoCountryDto Dto, string UserName) : IRequest<GeoCountryDto?>;
public sealed record DeleteCountryCommand(string Code, string UserName) : IRequest<bool>;

// ── Handlers ─────────────────────────────────────────────────────────────────
public sealed class GetCountriesHandler(IGeoCountryRepository repo) : IRequestHandler<GetCountriesQuery, IEnumerable<GeoCountryDto>>
{
    public async Task<IEnumerable<GeoCountryDto>> Handle(GetCountriesQuery request, CancellationToken ct)
    {
        var list = await repo.GetAllAsync(ct);
        return list.Select(e => new GeoCountryDto
        {
            Code = e.Id, ContinentCode = e.ContinentCode,
            NameVi = e.NameVi, NameEn = e.NameEn, NameFr = e.NameFr,
            Flag = e.Flag, Visible = e.Visible
        });
    }
}

public sealed class GetCountryByCodeHandler(IGeoCountryRepository repo) : IRequestHandler<GetCountryByCodeQuery, GeoCountryDto?>
{
    public async Task<GeoCountryDto?> Handle(GetCountryByCodeQuery request, CancellationToken ct)
    {
        var e = await repo.GetByCodeAsync(request.Code, ct);
        return e is null ? null : new GeoCountryDto
        {
            Code = e.Id, ContinentCode = e.ContinentCode,
            NameVi = e.NameVi, NameEn = e.NameEn, NameFr = e.NameFr,
            Flag = e.Flag, Visible = e.Visible
        };
    }
}

public sealed class CreateCountryHandler(IGeoCountryRepository repo) : IRequestHandler<CreateCountryCommand, GeoCountryDto>
{
    public async Task<GeoCountryDto> Handle(CreateCountryCommand request, CancellationToken ct)
    {
        var dto = request.Dto;
        var entity = GeoCountry.Create(dto.Code, dto.ContinentCode, dto.NameVi, dto.NameEn, dto.NameFr, dto.Visible, dto.Flag);
        entity.CreatedBy = request.UserName;
        await repo.AddAsync(entity, ct);
        return new GeoCountryDto
        {
            Code = entity.Id, ContinentCode = entity.ContinentCode,
            NameVi = entity.NameVi, NameEn = entity.NameEn, NameFr = entity.NameFr,
            Flag = entity.Flag, Visible = entity.Visible
        };
    }
}

public sealed class UpdateCountryHandler(IGeoCountryRepository repo) : IRequestHandler<UpdateCountryCommand, GeoCountryDto?>
{
    public async Task<GeoCountryDto?> Handle(UpdateCountryCommand request, CancellationToken ct)
    {
        var entity = await repo.GetByCodeAsync(request.Code, ct);
        if (entity is null) return null;

        entity.ContinentCode = request.Dto.ContinentCode;
        entity.NameVi = request.Dto.NameVi;
        entity.NameEn = request.Dto.NameEn;
        entity.NameFr = request.Dto.NameFr;
        entity.Flag = request.Dto.Flag;
        entity.Visible = request.Dto.Visible;
        entity.LastModifiedBy = request.UserName;
        repo.Update(entity);
        return new GeoCountryDto
        {
            Code = entity.Id, ContinentCode = entity.ContinentCode,
            NameVi = entity.NameVi, NameEn = entity.NameEn, NameFr = entity.NameFr,
            Flag = entity.Flag, Visible = entity.Visible
        };
    }
}

public sealed class DeleteCountryHandler(IGeoCountryRepository repo) : IRequestHandler<DeleteCountryCommand, bool>
{
    public async Task<bool> Handle(DeleteCountryCommand request, CancellationToken ct)
    {
        var entity = await repo.GetByCodeAsync(request.Code, ct);
        if (entity is null) return false;

        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedBy = request.UserName;
        repo.Update(entity);
        return true;
    }
}
