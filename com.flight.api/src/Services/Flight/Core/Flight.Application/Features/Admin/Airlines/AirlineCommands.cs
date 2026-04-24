using Flight.Application.Dtos;
using Flight.Domain.Aggregates.Booking;
using Flight.Domain.Repositories;
using MediatR;

namespace Flight.Application.Features.Admin.Airlines;

public sealed record GetAirlinesQuery(
    int Page = 1,
    int PageSize = 20,
    string? Code = null)
    : IRequest<PaginatedResult<AirlineDto>>;

public sealed record GetAirlineByIdQuery(int Id) : IRequest<AirlineDto?>;

public sealed record CreateAirlineCommand(string Code, string Name, string? Logo) : IRequest<AirlineDto>;

public sealed record UpdateAirlineCommand(int Id, string Name, string? Logo) : IRequest<AirlineDto?>;

public sealed record DeleteAirlineCommand(int Id) : IRequest<bool>;

public sealed class GetAirlinesHandler(IReferenceDataRepository repo) : IRequestHandler<GetAirlinesQuery, PaginatedResult<AirlineDto>>
{
    public async Task<PaginatedResult<AirlineDto>> Handle(GetAirlinesQuery request, CancellationToken ct)
    {
        var query = repo.GetAirlinesQuery();

        if (!string.IsNullOrEmpty(request.Code))
            query = query.Where(a => a.Code.Contains(request.Code));

        var total = query.Count();
        var list = query
            .OrderBy(a => a.Code)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new AirlineDto { Id = a.Id, Code = a.Code, Name = a.Name, Logo = a.Logo, Visible = a.Visible })
            .ToList();

        return new PaginatedResult<AirlineDto>(total, request.Page, request.PageSize, list);
    }
}

public sealed class GetAirlineByIdHandler(IReferenceDataRepository repo) : IRequestHandler<GetAirlineByIdQuery, AirlineDto?>
{
    public async Task<AirlineDto?> Handle(GetAirlineByIdQuery request, CancellationToken ct)
    {
        var entity = await repo.GetAirlineByIdAsync(request.Id, ct);
        return entity is null ? null : new AirlineDto { Id = entity.Id, Code = entity.Code, Name = entity.Name, Logo = entity.Logo, Visible = entity.Visible };
    }
}

public sealed class CreateAirlineHandler(IReferenceDataRepository repo) : IRequestHandler<CreateAirlineCommand, AirlineDto>
{
    public async Task<AirlineDto> Handle(CreateAirlineCommand request, CancellationToken ct)
    {
        var entity = AirlineEntity.Create(request.Code, request.Name, request.Logo);
        await repo.AddAirlineAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        return new AirlineDto { Id = entity.Id, Code = entity.Code, Name = entity.Name, Logo = entity.Logo, Visible = entity.Visible };
    }
}

public sealed class UpdateAirlineHandler(IReferenceDataRepository repo) : IRequestHandler<UpdateAirlineCommand, AirlineDto?>
{
    public async Task<AirlineDto?> Handle(UpdateAirlineCommand request, CancellationToken ct)
    {
        var entity = await repo.GetAirlineByIdAsync(request.Id, ct);
        if (entity is null) return null;

        entity.Name = request.Name;
        entity.Logo = request.Logo;
        await repo.SaveChangesAsync(ct);
        return new AirlineDto { Id = entity.Id, Code = entity.Code, Name = entity.Name, Logo = entity.Logo, Visible = entity.Visible };
    }
}

public sealed class DeleteAirlineHandler(IReferenceDataRepository repo) : IRequestHandler<DeleteAirlineCommand, bool>
{
    public async Task<bool> Handle(DeleteAirlineCommand request, CancellationToken ct)
    {
        var entity = await repo.GetAirlineByIdAsync(request.Id, ct);
        if (entity is null) return false;

        entity.Visible = false;
        await repo.SaveChangesAsync(ct);
        return true;
    }
}

public sealed record PaginatedResult<T>(int Total, int Page, int PageSize, IReadOnlyList<T> Data);