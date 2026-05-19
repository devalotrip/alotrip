using Flight.Application.Dtos;
using Flight.Domain.Aggregates.Geo;
using Flight.Domain.Repositories;
using MediatR;

namespace Flight.Application.Features.Admin.Geo;

// ── Queries ──────────────────────────────────────────────────────────────────
public sealed record GetContinentsQuery : IRequest<IEnumerable<GeoContinentDto>>;
public sealed record GetContinentByCodeQuery(string Code) : IRequest<GeoContinentDto?>;

// ── Commands ─────────────────────────────────────────────────────────────────
public sealed record CreateContinentCommand(GeoContinentDto Dto, string UserName) : IRequest<GeoContinentDto>;
public sealed record UpdateContinentCommand(string Code, GeoContinentDto Dto, string UserName) : IRequest<GeoContinentDto?>;
public sealed record DeleteContinentCommand(string Code, string UserName) : IRequest<bool>;

// ── Handlers ─────────────────────────────────────────────────────────────────
public sealed class GetContinentsHandler(IGeoContinentRepository repo) : IRequestHandler<GetContinentsQuery, IEnumerable<GeoContinentDto>>
{
    public async Task<IEnumerable<GeoContinentDto>> Handle(GetContinentsQuery request, CancellationToken ct)
    {
        var list = await repo.GetAllAsync(ct);
        return list.Select(e => new GeoContinentDto
        {
            Code = e.Id, NameVi = e.NameVi, NameEn = e.NameEn, NameFr = e.NameFr, Visible = e.Visible
        });
    }
}

public sealed class GetContinentByCodeHandler(IGeoContinentRepository repo) : IRequestHandler<GetContinentByCodeQuery, GeoContinentDto?>
{
    public async Task<GeoContinentDto?> Handle(GetContinentByCodeQuery request, CancellationToken ct)
    {
        var e = await repo.GetByCodeAsync(request.Code, ct);
        return e is null ? null : new GeoContinentDto
        {
            Code = e.Id, NameVi = e.NameVi, NameEn = e.NameEn, NameFr = e.NameFr, Visible = e.Visible
        };
    }
}

public sealed class CreateContinentHandler(IGeoContinentRepository repo) : IRequestHandler<CreateContinentCommand, GeoContinentDto>
{
    public async Task<GeoContinentDto> Handle(CreateContinentCommand request, CancellationToken ct)
    {
        var dto = request.Dto;
        var entity = GeoContinent.Create(dto.Code, dto.NameVi, dto.NameEn, dto.NameFr, dto.Visible);
        entity.CreatedBy = request.UserName;
        await repo.AddAsync(entity, ct);
        return new GeoContinentDto
        {
            Code = entity.Id, NameVi = entity.NameVi, NameEn = entity.NameEn, NameFr = entity.NameFr, Visible = entity.Visible
        };
    }
}

public sealed class UpdateContinentHandler(IGeoContinentRepository repo) : IRequestHandler<UpdateContinentCommand, GeoContinentDto?>
{
    public async Task<GeoContinentDto?> Handle(UpdateContinentCommand request, CancellationToken ct)
    {
        var entity = await repo.GetByCodeAsync(request.Code, ct);
        if (entity is null) return null;

        entity.NameVi = request.Dto.NameVi;
        entity.NameEn = request.Dto.NameEn;
        entity.NameFr = request.Dto.NameFr;
        entity.Visible = request.Dto.Visible;
        entity.LastModifiedBy = request.UserName;
        repo.Update(entity);
        return new GeoContinentDto
        {
            Code = entity.Id, NameVi = entity.NameVi, NameEn = entity.NameEn, NameFr = entity.NameFr, Visible = entity.Visible
        };
    }
}

public sealed class DeleteContinentHandler(IGeoContinentRepository repo) : IRequestHandler<DeleteContinentCommand, bool>
{
    public async Task<bool> Handle(DeleteContinentCommand request, CancellationToken ct)
    {
        var entity = await repo.GetByCodeAsync(request.Code, ct);
        if (entity is null) return false;

        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedBy = request.UserName;
        repo.Update(entity);
        return true;
    }
}
