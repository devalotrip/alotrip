using Flight.Application.Dtos;
using Flight.Application.Features.Admin.Airlines;
using Flight.Domain.Aggregates.Booking;
using Flight.Domain.Repositories;
using MediatR;

namespace Flight.Application.Features.Admin.PassengerTypes;

// ── Queries & Commands ──────────────────────────────────────────────────────

public sealed record GetPassengerTypesQuery(int Page = 1, int PageSize = 20, string? Code = null)
    : IRequest<PaginatedResult<PassengerTypeDto>>;

public sealed record GetPassengerTypeByCodeQuery(string Code) : IRequest<PassengerTypeDto?>;

public sealed record CreatePassengerTypeCommand(
    string Code, string? Icon, string? NameVi, string? NameEn, string? NameFr, string? Description)
    : IRequest<PassengerTypeDto>;

public sealed record UpdatePassengerTypeCommand(
    string Code, string? Icon, string? NameVi, string? NameEn, string? NameFr, string? Description)
    : IRequest<PassengerTypeDto?>;

public sealed record DeletePassengerTypeCommand(string Code) : IRequest<bool>;

// ── Handlers ────────────────────────────────────────────────────────────────

public sealed class GetPassengerTypesHandler(IReferenceDataRepository repo)
    : IRequestHandler<GetPassengerTypesQuery, PaginatedResult<PassengerTypeDto>>
{
    public async Task<PaginatedResult<PassengerTypeDto>> Handle(GetPassengerTypesQuery request, CancellationToken ct)
    {
        var query = repo.GetPassengerTypesQuery();
        if (!string.IsNullOrWhiteSpace(request.Code))
            query = query.Where(p => p.Code.Contains(request.Code));

        var total = query.Count();
        var list = query
            .OrderBy(p => p.Code)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => PassengerTypeMapping.ToDto(p))
            .ToList();

        return new PaginatedResult<PassengerTypeDto>(total, request.Page, request.PageSize, list);
    }
}

public sealed class GetPassengerTypeByCodeHandler(IReferenceDataRepository repo)
    : IRequestHandler<GetPassengerTypeByCodeQuery, PassengerTypeDto?>
{
    public async Task<PassengerTypeDto?> Handle(GetPassengerTypeByCodeQuery request, CancellationToken ct)
    {
        var entity = await repo.GetPassengerTypeByCodeAsync(request.Code, ct);
        return entity is null ? null : PassengerTypeMapping.ToDto(entity);
    }
}

public sealed class CreatePassengerTypeHandler(IReferenceDataRepository repo)
    : IRequestHandler<CreatePassengerTypeCommand, PassengerTypeDto>
{
    public async Task<PassengerTypeDto> Handle(CreatePassengerTypeCommand request, CancellationToken ct)
    {
        // Check if code already exists
        var existing = await repo.GetPassengerTypeByCodeAsync(request.Code, ct);
        if (existing is not null)
            throw new InvalidOperationException($"PassengerType with code '{request.Code}' already exists.");

        var entity = PassengerTypeEntity.Create(
            request.Code, request.Icon, request.NameVi, request.NameEn, request.NameFr, request.Description);
        await repo.AddPassengerTypeAsync(entity, ct);
        return PassengerTypeMapping.ToDto(entity);
    }
}

public sealed class UpdatePassengerTypeHandler(IReferenceDataRepository repo)
    : IRequestHandler<UpdatePassengerTypeCommand, PassengerTypeDto?>
{
    public async Task<PassengerTypeDto?> Handle(UpdatePassengerTypeCommand request, CancellationToken ct)
    {
        var entity = await repo.GetPassengerTypeByCodeAsync(request.Code, ct);
        if (entity is null) return null;
        entity.Update(request.Icon, request.NameVi, request.NameEn, request.NameFr, request.Description);
        await repo.SaveChangesAsync(ct);
        return PassengerTypeMapping.ToDto(entity);
    }
}

public sealed class DeletePassengerTypeHandler(IReferenceDataRepository repo)
    : IRequestHandler<DeletePassengerTypeCommand, bool>
{
    public async Task<bool> Handle(DeletePassengerTypeCommand request, CancellationToken ct)
    {
        var entity = await repo.GetPassengerTypeByCodeAsync(request.Code, ct);
        if (entity is null) return false;
        repo.RemovePassengerType(entity);
        await repo.SaveChangesAsync(ct);
        return true;
    }
}

// ── Mapping helper ──────────────────────────────────────────────────────────

file static class PassengerTypeMapping
{
    public static PassengerTypeDto ToDto(PassengerTypeEntity e)
        => new()
        {
            Code = e.Code,
            Icon = e.Icon,
            NameVi = e.NameVi,
            NameEn = e.NameEn,
            NameFr = e.NameFr,
            Description = e.Description,
        };
}
