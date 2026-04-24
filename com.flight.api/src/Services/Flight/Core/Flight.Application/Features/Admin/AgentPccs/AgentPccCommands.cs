using Flight.Application.Dtos;
using Flight.Domain.Aggregates.Booking;
using Flight.Domain.Repositories;
using MediatR;

namespace Flight.Application.Features.Admin.AgentPccs;

public sealed record GetAgentPccsQuery(int? AgentId = null, int Page = 1, int PageSize = 20) : IRequest<AgentPccsResponse>;
public sealed record GetAgentPccByIdQuery(int Id) : IRequest<AgentPccDto?>;
public sealed record CreateAgentPccCommand(int AgentId, string Pcc, int IgnoredMode, string? ListStartPoint) : IRequest<AgentPccDto?>;
public sealed record UpdateAgentPccCommand(int Id, int IgnoredMode, string? ListStartPoint) : IRequest<AgentPccDto?>;
public sealed record DeleteAgentPccCommand(int Id) : IRequest<bool>;

public sealed record AgentPccsResponse(int Total, int Page, int PageSize, IEnumerable<AgentPccDto> Data);

public sealed class GetAgentPccsHandler(IAgentRepository repo) : IRequestHandler<GetAgentPccsQuery, AgentPccsResponse>
{
    public async Task<AgentPccsResponse> Handle(GetAgentPccsQuery request, CancellationToken ct)
    {
        var query = repo.GetAgentPccsQuery().Where(a => a.Active);
        if (request.AgentId.HasValue) query = query.Where(a => a.AgentId == request.AgentId);
        var total = await Task.Run(() => query.Count(), ct);
        var list = query.OrderBy(a => a.Pcc).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToList();
        return new AgentPccsResponse(total, request.Page, request.PageSize, list.Select(e => new AgentPccDto
        {
            Id = e.Id, AgentId = e.AgentId, Pcc = e.Pcc,
            IgnoredMode = e.IgnoredMode, ListStartPoint = e.ListStartPoint, Active = e.Active
        }));
    }
}

public sealed class GetAgentPccByIdHandler(IAgentRepository repo) : IRequestHandler<GetAgentPccByIdQuery, AgentPccDto?>
{
    public async Task<AgentPccDto?> Handle(GetAgentPccByIdQuery request, CancellationToken ct)
    {
        var entity = await repo.GetAgentPccByIdAsync(request.Id, ct);
        return entity is null ? null : new AgentPccDto
        {
            Id = entity.Id, AgentId = entity.AgentId, Pcc = entity.Pcc,
            IgnoredMode = entity.IgnoredMode, ListStartPoint = entity.ListStartPoint, Active = entity.Active
        };
    }
}

public sealed class CreateAgentPccHandler(IAgentRepository repo) : IRequestHandler<CreateAgentPccCommand, AgentPccDto?>
{
    public async Task<AgentPccDto?> Handle(CreateAgentPccCommand request, CancellationToken ct)
    {
        var exists = await repo.AgentPccExistsAsync(request.AgentId, request.Pcc, ct);
        if (exists) return null;
        var entity = AgentPccEntity.Create(request.AgentId, request.Pcc, request.IgnoredMode, request.ListStartPoint);
        await repo.AddAgentPccAsync(entity, ct);
        return new AgentPccDto
        {
            Id = entity.Id, AgentId = entity.AgentId, Pcc = entity.Pcc,
            IgnoredMode = entity.IgnoredMode, ListStartPoint = entity.ListStartPoint, Active = entity.Active
        };
    }
}

public sealed class UpdateAgentPccHandler(IAgentRepository repo) : IRequestHandler<UpdateAgentPccCommand, AgentPccDto?>
{
    public async Task<AgentPccDto?> Handle(UpdateAgentPccCommand request, CancellationToken ct)
    {
        var entity = await repo.GetAgentPccByIdAsync(request.Id, ct);
        if (entity is null) return null;
        entity.IgnoredMode = request.IgnoredMode;
        entity.ListStartPoint = request.ListStartPoint;
        await repo.SaveChangesAsync(ct);
        return new AgentPccDto
        {
            Id = entity.Id, AgentId = entity.AgentId, Pcc = entity.Pcc,
            IgnoredMode = entity.IgnoredMode, ListStartPoint = entity.ListStartPoint, Active = entity.Active
        };
    }
}

public sealed class DeleteAgentPccHandler(IAgentRepository repo) : IRequestHandler<DeleteAgentPccCommand, bool>
{
    public async Task<bool> Handle(DeleteAgentPccCommand request, CancellationToken ct)
        => await repo.DeleteAgentPccAsync(request.Id, ct);
}