using Flight.Application.Dtos;
using Flight.Application.Features.Admin.Airlines;
using Flight.Domain.Aggregates.Booking;
using Flight.Domain.Repositories;
using MediatR;
using System.Linq;

namespace Flight.Application.Features.Admin.AgentPartners;

public sealed record GetAgentPartnersQuery(int Page = 1, int PageSize = 20, int? AgentId = null, int? PartnerId = null) : IRequest<PaginatedResult<AgentPartnerDto>>;
public sealed record CreateAgentPartnerCommand(int AgentId, int PartnerId, int IgnoredMode, string? ListStartPoint) : IRequest<AgentPartnerDto>;
public sealed record DeleteAgentPartnerCommand(int Id) : IRequest<bool>;

public sealed class GetAgentPartnersHandler(IAgentRepository repo) : IRequestHandler<GetAgentPartnersQuery, PaginatedResult<AgentPartnerDto>>
{
    public async Task<PaginatedResult<AgentPartnerDto>> Handle(GetAgentPartnersQuery request, CancellationToken ct)
    {
        var query = repo.GetAgentPartnersQuery();
        if (request.AgentId.HasValue) query = query.Where(a => a.AgentId == request.AgentId);
        if (request.PartnerId.HasValue) query = query.Where(a => a.PartnerId == request.PartnerId);
        var total = query.Count();
        var list = query.OrderByDescending(a => a.CreatedOnUtc).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(a => new AgentPartnerDto { Id = a.Id, AgentId = a.AgentId, PartnerId = a.PartnerId, IgnoredMode = a.IgnoredMode, ListStartPoint = a.ListStartPoint, Active = a.Active }).ToList();
        return new PaginatedResult<AgentPartnerDto>(total, request.Page, request.PageSize, list);
    }
}

public sealed class CreateAgentPartnerHandler(IAgentRepository repo) : IRequestHandler<CreateAgentPartnerCommand, AgentPartnerDto>
{
    public async Task<AgentPartnerDto> Handle(CreateAgentPartnerCommand request, CancellationToken ct)
    {
        var entity = AgentPartnerEntity.Create(request.AgentId, request.PartnerId, request.IgnoredMode, request.ListStartPoint);
        await repo.AddAgentPartnerAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        return new AgentPartnerDto { Id = entity.Id, AgentId = entity.AgentId, PartnerId = entity.PartnerId, IgnoredMode = entity.IgnoredMode, ListStartPoint = entity.ListStartPoint, Active = entity.Active };
    }
}

public sealed class DeleteAgentPartnerHandler(IAgentRepository repo) : IRequestHandler<DeleteAgentPartnerCommand, bool>
{
    public async Task<bool> Handle(DeleteAgentPartnerCommand request, CancellationToken ct)
    {
        var entity = await repo.GetAgentPartnerByIdAsync(request.Id, ct);
        if (entity is null) return false;
        entity.Active = false;
        await repo.SaveChangesAsync(ct);
        return true;
    }
}