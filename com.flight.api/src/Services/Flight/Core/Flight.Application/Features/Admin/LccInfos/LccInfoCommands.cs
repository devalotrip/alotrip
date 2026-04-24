using Flight.Application.Dtos;
using Flight.Domain.Aggregates.Booking;
using Flight.Domain.Repositories;
using MediatR;

namespace Flight.Application.Features.Admin.LccInfos;

public sealed record GetLccInfosQuery(int? AgentId = null, int Page = 1, int PageSize = 20) : IRequest<LccInfosResponse>;
public sealed record GetLccInfoByIdQuery(int Id) : IRequest<LccInfoDto?>;
public sealed record CreateLccInfoCommand(int AgentId, string Airline, bool AllowSearch, bool AllowBook) : IRequest<LccInfoDto?>;
public sealed record UpdateLccInfoCommand(int Id, bool? AllowSearch = null, bool? AllowBook = null, string? ProxyServerId = null, string? ProxyServerBookId = null) : IRequest<LccInfoDto?>;
public sealed record DeleteLccInfoCommand(int Id) : IRequest<bool>;

public sealed record LccInfosResponse(int Total, int Page, int PageSize, IEnumerable<LccInfoDto> Data);

public sealed class GetLccInfosHandler(IAgentRepository repo) : IRequestHandler<GetLccInfosQuery, LccInfosResponse>
{
    public async Task<LccInfosResponse> Handle(GetLccInfosQuery request, CancellationToken ct)
    {
        var query = repo.GetLccInfosQuery().Where(l => l.DeletedAt == null);
        if (request.AgentId.HasValue) query = query.Where(l => l.AgentId == request.AgentId);
        var total = await Task.Run(() => query.Count(), ct);
        var list = query.OrderBy(l => l.Airline).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToList();
        return new LccInfosResponse(total, request.Page, request.PageSize, list.Select(e => new LccInfoDto
        {
            Id = e.Id, AgentId = e.AgentId, Airline = e.Airline,
            AllowSearch = e.AllowSearch, AllowBook = e.AllowBook,
            ProxyServerId = e.ProxyServerId, ProxyServerBookId = e.ProxyServerBookId
        }));
    }
}

public sealed class GetLccInfoByIdHandler(IAgentRepository repo) : IRequestHandler<GetLccInfoByIdQuery, LccInfoDto?>
{
    public async Task<LccInfoDto?> Handle(GetLccInfoByIdQuery request, CancellationToken ct)
    {
        var entity = await repo.GetLccInfoByIdAsync(request.Id, ct);
        return entity is null ? null : new LccInfoDto
        {
            Id = entity.Id, AgentId = entity.AgentId, Airline = entity.Airline,
            AllowSearch = entity.AllowSearch, AllowBook = entity.AllowBook,
            ProxyServerId = entity.ProxyServerId, ProxyServerBookId = entity.ProxyServerBookId
        };
    }
}

public sealed class CreateLccInfoHandler(IAgentRepository repo) : IRequestHandler<CreateLccInfoCommand, LccInfoDto?>
{
    public async Task<LccInfoDto?> Handle(CreateLccInfoCommand request, CancellationToken ct)
    {
        var exists = await repo.LccInfoExistsAsync(request.AgentId, request.Airline, ct);
        if (exists) return null;
        var entity = LccInfoEntity.Create(request.AgentId, request.Airline, request.AllowSearch, request.AllowBook);
        await repo.AddLccInfoAsync(entity, ct);
        return new LccInfoDto
        {
            Id = entity.Id, AgentId = entity.AgentId, Airline = entity.Airline,
            AllowSearch = entity.AllowSearch, AllowBook = entity.AllowBook
        };
    }
}

public sealed class UpdateLccInfoHandler(IAgentRepository repo) : IRequestHandler<UpdateLccInfoCommand, LccInfoDto?>
{
    public async Task<LccInfoDto?> Handle(UpdateLccInfoCommand request, CancellationToken ct)
    {
        var entity = await repo.GetLccInfoByIdAsync(request.Id, ct);
        if (entity is null) return null;
        if (request.AllowSearch.HasValue) entity.AllowSearch = request.AllowSearch.Value;
        if (request.AllowBook.HasValue) entity.AllowBook = request.AllowBook.Value;
        if (request.ProxyServerId != null) entity.ProxyServerId = request.ProxyServerId;
        if (request.ProxyServerBookId != null) entity.ProxyServerBookId = request.ProxyServerBookId;
        await repo.SaveChangesAsync(ct);
        return new LccInfoDto
        {
            Id = entity.Id, AgentId = entity.AgentId, Airline = entity.Airline,
            AllowSearch = entity.AllowSearch, AllowBook = entity.AllowBook,
            ProxyServerId = entity.ProxyServerId, ProxyServerBookId = entity.ProxyServerBookId
        };
    }
}

public sealed class DeleteLccInfoHandler(IAgentRepository repo) : IRequestHandler<DeleteLccInfoCommand, bool>
{
    public async Task<bool> Handle(DeleteLccInfoCommand request, CancellationToken ct)
        => await repo.DeleteLccInfoAsync(request.Id, ct);
}