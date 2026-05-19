using Flight.Application.Dtos;
using Flight.Application.Features.Admin.Airlines;
using Flight.Domain.Aggregates.Booking;
using Flight.Domain.Repositories;
using MediatR;
using System.Linq;

namespace Flight.Application.Features.Admin.AirlineTypes;

public sealed record GetAirlineTypesQuery(int Page = 1, int PageSize = 20, string? Code = null) : IRequest<PaginatedResult<AirlineTypeDto>>;
public sealed record GetAirlineTypeByIdQuery(int Id) : IRequest<AirlineTypeDto?>;
public sealed record CreateAirlineTypeCommand(string Code, string Name, string? Description) : IRequest<AirlineTypeDto>;
public sealed record UpdateAirlineTypeCommand(int Id, string Name, string? Description) : IRequest<AirlineTypeDto?>;
public sealed record DeleteAirlineTypeCommand(int Id) : IRequest<bool>;

public sealed class GetAirlineTypesHandler(IReferenceDataRepository repo) : IRequestHandler<GetAirlineTypesQuery, PaginatedResult<AirlineTypeDto>>
{
    public async Task<PaginatedResult<AirlineTypeDto>> Handle(GetAirlineTypesQuery request, CancellationToken ct)
    {
        var query = repo.GetAirlineTypesQuery();
        if (!string.IsNullOrEmpty(request.Code)) query = query.Where(a => a.Code.Contains(request.Code));
        var total = query.Count();
        var list = query.OrderBy(a => a.Code).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(a => new AirlineTypeDto { Id = a.Id, Code = a.Code, Name = a.Name, Description = a.Description, Visible = a.Visible }).ToList();
        return new PaginatedResult<AirlineTypeDto>(total, request.Page, request.PageSize, list);
    }
}

public sealed class GetAirlineTypeByIdHandler(IReferenceDataRepository repo) : IRequestHandler<GetAirlineTypeByIdQuery, AirlineTypeDto?>
{
    public async Task<AirlineTypeDto?> Handle(GetAirlineTypeByIdQuery request, CancellationToken ct)
    {
        var entity = await repo.GetAirlineTypeByIdAsync(request.Id, ct);
        return entity is null ? null : new AirlineTypeDto { Id = entity.Id, Code = entity.Code, Name = entity.Name, Description = entity.Description, Visible = entity.Visible };
    }
}

public sealed class CreateAirlineTypeHandler(IReferenceDataRepository repo) : IRequestHandler<CreateAirlineTypeCommand, AirlineTypeDto>
{
    public async Task<AirlineTypeDto> Handle(CreateAirlineTypeCommand request, CancellationToken ct)
    {
        var entity = AirlineTypeEntity.Create(request.Code, request.Name, request.Description);
        await repo.AddAirlineTypeAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        return new AirlineTypeDto { Id = entity.Id, Code = entity.Code, Name = entity.Name, Description = entity.Description, Visible = entity.Visible };
    }
}

public sealed class UpdateAirlineTypeHandler(IReferenceDataRepository repo) : IRequestHandler<UpdateAirlineTypeCommand, AirlineTypeDto?>
{
    public async Task<AirlineTypeDto?> Handle(UpdateAirlineTypeCommand request, CancellationToken ct)
    {
        var entity = await repo.GetAirlineTypeByIdAsync(request.Id, ct);
        if (entity is null) return null;
        entity.Name = request.Name;
        entity.Description = request.Description;
        await repo.SaveChangesAsync(ct);
        return new AirlineTypeDto { Id = entity.Id, Code = entity.Code, Name = entity.Name, Description = entity.Description, Visible = entity.Visible };
    }
}

public sealed class DeleteAirlineTypeHandler(IReferenceDataRepository repo) : IRequestHandler<DeleteAirlineTypeCommand, bool>
{
    public async Task<bool> Handle(DeleteAirlineTypeCommand request, CancellationToken ct)
    {
        var entity = await repo.GetAirlineTypeByIdAsync(request.Id, ct);
        if (entity is null) return false;
        entity.Visible = false;
        await repo.SaveChangesAsync(ct);
        return true;
    }
}