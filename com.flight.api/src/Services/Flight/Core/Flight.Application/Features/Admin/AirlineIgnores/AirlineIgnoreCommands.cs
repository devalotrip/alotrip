using Flight.Application.Dtos;
using Flight.Application.Features.Admin.Airlines;
using Flight.Domain.Aggregates.Booking;
using Flight.Domain.Repositories;
using MediatR;
using System.Linq;

namespace Flight.Application.Features.Admin.AirlineIgnores;

public sealed record GetAirlineIgnoresQuery(int Page = 1, int PageSize = 20, int? AgentId = null) : IRequest<PaginatedResult<AirlineIgnoreAdminDto>>;
public sealed record GetAirlineIgnoreByIdQuery(int Id) : IRequest<AirlineIgnoreAdminDto?>;
public sealed record CreateAirlineIgnoreCommand(int AgentId, string Airline, bool FilterByPlatingCarrier, bool FilterByAnySegment, bool FilterByAllSegment) : IRequest<AirlineIgnoreAdminDto>;
public sealed record UpdateAirlineIgnoreCommand(int Id, bool FilterByPlatingCarrier, bool FilterByAnySegment, bool FilterByAllSegment) : IRequest<AirlineIgnoreAdminDto?>;
public sealed record DeleteAirlineIgnoreCommand(int Id) : IRequest<bool>;

public sealed class GetAirlineIgnoresHandler(IAgentRepository repo) : IRequestHandler<GetAirlineIgnoresQuery, PaginatedResult<AirlineIgnoreAdminDto>>
{
    public async Task<PaginatedResult<AirlineIgnoreAdminDto>> Handle(GetAirlineIgnoresQuery request, CancellationToken ct)
    {
        var query = repo.GetAirlineIgnoresQuery();
        if (request.AgentId.HasValue) query = query.Where(a => a.AgentId == request.AgentId);
        var total = query.Count();
        var list = query.OrderBy(a => a.Airline).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(a => new AirlineIgnoreAdminDto { Id = a.Id, AgentId = a.AgentId, Airline = a.Airline, FilterByPlatingCarrier = a.FilterByPlatingCarrier, FilterByAnySegment = a.FilterByAnySegment, FilterByAllSegment = a.FilterByAllSegment }).ToList();
        return new PaginatedResult<AirlineIgnoreAdminDto>(total, request.Page, request.PageSize, list);
    }
}

public sealed class GetAirlineIgnoreByIdHandler(IAgentRepository repo) : IRequestHandler<GetAirlineIgnoreByIdQuery, AirlineIgnoreAdminDto?>
{
    public async Task<AirlineIgnoreAdminDto?> Handle(GetAirlineIgnoreByIdQuery request, CancellationToken ct)
    {
        var entity = await repo.GetAirlineIgnoreByIdAsync(request.Id, ct);
        return entity is null ? null : new AirlineIgnoreAdminDto { Id = entity.Id, AgentId = entity.AgentId, Airline = entity.Airline, FilterByPlatingCarrier = entity.FilterByPlatingCarrier, FilterByAnySegment = entity.FilterByAnySegment, FilterByAllSegment = entity.FilterByAllSegment };
    }
}

public sealed class CreateAirlineIgnoreHandler(IAgentRepository repo) : IRequestHandler<CreateAirlineIgnoreCommand, AirlineIgnoreAdminDto>
{
    public async Task<AirlineIgnoreAdminDto> Handle(CreateAirlineIgnoreCommand request, CancellationToken ct)
    {
        var entity = AirlineIgnoreEntity.Create(request.AgentId, request.Airline, request.FilterByPlatingCarrier, request.FilterByAnySegment, request.FilterByAllSegment);
        await repo.AddAirlineIgnoreAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        return new AirlineIgnoreAdminDto { Id = entity.Id, AgentId = entity.AgentId, Airline = entity.Airline, FilterByPlatingCarrier = entity.FilterByPlatingCarrier, FilterByAnySegment = entity.FilterByAnySegment, FilterByAllSegment = entity.FilterByAllSegment };
    }
}

public sealed class UpdateAirlineIgnoreHandler(IAgentRepository repo) : IRequestHandler<UpdateAirlineIgnoreCommand, AirlineIgnoreAdminDto?>
{
    public async Task<AirlineIgnoreAdminDto?> Handle(UpdateAirlineIgnoreCommand request, CancellationToken ct)
    {
        var entity = await repo.GetAirlineIgnoreByIdAsync(request.Id, ct);
        if (entity is null) return null;
        entity.FilterByPlatingCarrier = request.FilterByPlatingCarrier;
        entity.FilterByAnySegment = request.FilterByAnySegment;
        entity.FilterByAllSegment = request.FilterByAllSegment;
        await repo.SaveChangesAsync(ct);
        return new AirlineIgnoreAdminDto { Id = entity.Id, AgentId = entity.AgentId, Airline = entity.Airline, FilterByPlatingCarrier = entity.FilterByPlatingCarrier, FilterByAnySegment = entity.FilterByAnySegment, FilterByAllSegment = entity.FilterByAllSegment };
    }
}

public sealed class DeleteAirlineIgnoreHandler(IAgentRepository repo) : IRequestHandler<DeleteAirlineIgnoreCommand, bool>
{
    public async Task<bool> Handle(DeleteAirlineIgnoreCommand request, CancellationToken ct)
    {
        var entity = await repo.GetAirlineIgnoreByIdAsync(request.Id, ct);
        if (entity is null) return false;
        entity.DeletedAt = DateTime.UtcNow;
        await repo.SaveChangesAsync(ct);
        return true;
    }
}