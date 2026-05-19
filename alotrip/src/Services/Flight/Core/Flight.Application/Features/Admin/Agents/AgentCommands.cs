using Flight.Domain.Aggregates.Booking;
using Flight.Domain.Repositories;
using MediatR;

namespace Flight.Application.Features.Admin.Agents;

// ── Queries ──────────────────────────────────────────────────────────────────
public sealed record GetAgentsQuery(int Page = 1, int PageSize = 50, bool? Active = null) : IRequest<IEnumerable<AgentDto>>;
public sealed record GetAgentByIdQuery(int Id) : IRequest<AgentDetailDto?>;

// ── Existing commands ────────────────────────────────────────────────────────
public sealed record ToggleAgentActiveCommand(int Id, bool Active) : IRequest<bool>;
public sealed record ChangeAgentPasswordCommand(int Id, string PasswordHash) : IRequest<bool>;

// ── New CRUD commands ────────────────────────────────────────────────────────
public sealed record CreateAgentCommand(
    string AgentCode, string Name, string Email, string PasswordHash,
    string? Address = null, string? Tel = null,
    bool LccVnActiveDomestic = false, bool LccVnActiveGlobal = false,
    string? GalileoPcc = null, bool GalileoActive = false,
    string? DefaultCurrency = "VND", bool EnableCache = false, int CacheTimeMinutes = 15,
    bool SendMailInApi = false, int EmailSender = 0, int CombinedMode = 0,
    DateTime? ExpiryDate = null,
    decimal BaggageFeePercent = 0, decimal BaggageFeeAmount = 0) : IRequest<AgentDetailDto>;

public sealed record UpdateAgentCommand(
    int Id,
    string Name, string Email,
    string? Address = null, string? Tel = null,
    bool LccVnActiveDomestic = false, bool LccVnActiveGlobal = false,
    string? GalileoPcc = null, bool GalileoActive = false,
    string? DefaultCurrency = "VND", bool EnableCache = false, int CacheTimeMinutes = 15,
    bool SendMailInApi = false, int EmailSender = 0, int CombinedMode = 0,
    DateTime? ExpiryDate = null, bool Active = true,
    decimal BaggageFeePercent = 0, decimal BaggageFeeAmount = 0) : IRequest<AgentDetailDto?>;

public sealed record DeleteAgentCommand(int Id) : IRequest<bool>;

// ── Handlers ─────────────────────────────────────────────────────────────────

public sealed class GetAgentsHandler(IAgentRepository repo) : IRequestHandler<GetAgentsQuery, IEnumerable<AgentDto>>
{
    public async Task<IEnumerable<AgentDto>> Handle(GetAgentsQuery request, CancellationToken ct)
        => await repo.GetAgentsAsync(request.Page, request.PageSize, request.Active, ct);
}

public sealed class GetAgentByIdHandler(IAgentRepository repo) : IRequestHandler<GetAgentByIdQuery, AgentDetailDto?>
{
    public async Task<AgentDetailDto?> Handle(GetAgentByIdQuery request, CancellationToken ct)
        => await repo.GetAgentByIdAsync(request.Id, ct);
}

public sealed class ToggleAgentActiveHandler(IAgentRepository repo) : IRequestHandler<ToggleAgentActiveCommand, bool>
{
    public async Task<bool> Handle(ToggleAgentActiveCommand request, CancellationToken ct)
        => await repo.ToggleAgentActiveAsync(request.Id, request.Active, ct);
}

public sealed class ChangeAgentPasswordHandler(IAgentRepository repo) : IRequestHandler<ChangeAgentPasswordCommand, bool>
{
    public async Task<bool> Handle(ChangeAgentPasswordCommand request, CancellationToken ct)
        => await repo.ChangeAgentPasswordAsync(request.Id, request.PasswordHash, ct);
}

public sealed class CreateAgentHandler(IAgentRepository repo) : IRequestHandler<CreateAgentCommand, AgentDetailDto>
{
    public async Task<AgentDetailDto> Handle(CreateAgentCommand req, CancellationToken ct)
    {
        // Check for duplicate AgentCode
        if (await repo.AgentCodeExistsAsync(req.AgentCode, ct))
            throw new InvalidOperationException($"AgentCode '{req.AgentCode}' already exists.");

        var entity = AgentEntity.Create(
            agentCode: req.AgentCode,
            name: req.Name,
            email: req.Email,
            passwordHash: req.PasswordHash,
            address: req.Address,
            tel: req.Tel,
            lccVnActiveDomestic: req.LccVnActiveDomestic,
            lccVnActiveGlobal: req.LccVnActiveGlobal,
            galileoPcc: req.GalileoPcc,
            galileoActive: req.GalileoActive,
            defaultCurrency: req.DefaultCurrency,
            enableCache: req.EnableCache,
            cacheTimeMinutes: req.CacheTimeMinutes,
            sendMailInApi: req.SendMailInApi,
            emailSender: req.EmailSender,
            combinedMode: req.CombinedMode,
            expiryDate: req.ExpiryDate,
            baggageFeePercent: req.BaggageFeePercent,
            baggageFeeAmount: req.BaggageFeeAmount);

        await repo.CreateAgentAsync(entity, ct);
        return AgentMapping.MapToDetailDto(entity);
    }
}

public sealed class UpdateAgentHandler(IAgentRepository repo) : IRequestHandler<UpdateAgentCommand, AgentDetailDto?>
{
    public async Task<AgentDetailDto?> Handle(UpdateAgentCommand req, CancellationToken ct)
    {
        var entity = await repo.GetAgentEntityByIdAsync(req.Id, ct);
        if (entity is null) return null;

        entity.Update(
            name: req.Name,
            email: req.Email,
            address: req.Address,
            tel: req.Tel,
            lccVnActiveDomestic: req.LccVnActiveDomestic,
            lccVnActiveGlobal: req.LccVnActiveGlobal,
            galileoPcc: req.GalileoPcc,
            galileoActive: req.GalileoActive,
            defaultCurrency: req.DefaultCurrency,
            enableCache: req.EnableCache,
            cacheTimeMinutes: req.CacheTimeMinutes,
            sendMailInApi: req.SendMailInApi,
            emailSender: req.EmailSender,
            combinedMode: req.CombinedMode,
            expiryDate: req.ExpiryDate ?? entity.ExpiryDate,
            active: req.Active,
            baggageFeePercent: req.BaggageFeePercent,
            baggageFeeAmount: req.BaggageFeeAmount);

        await repo.UpdateAgentAsync(entity, ct);
        return AgentMapping.MapToDetailDto(entity);
    }
}

public sealed class DeleteAgentHandler(IAgentRepository repo) : IRequestHandler<DeleteAgentCommand, bool>
{
    public async Task<bool> Handle(DeleteAgentCommand request, CancellationToken ct)
        => await repo.DeleteAgentAsync(request.Id, ct);
}

// ── Shared mapping helper ────────────────────────────────────────────────────
file static class AgentMapping
{
    public static AgentDetailDto MapToDetailDto(AgentEntity e) => new()
    {
        Id = e.Id,
        AgentCode = e.AgentCode,
        Name = e.Name,
        Email = e.Email,
        Tel = e.Tel,
        Address = e.Address,
        Active = e.Active,
        GalileoPcc = e.GalileoPcc,
        GalileoActive = e.GalileoActive,
        LccVnActiveDomestic = e.LccVnActiveDomestic,
        LccVnActiveGlobal = e.LccVnActiveGlobal,
        EnableCache = e.EnableCache,
        CacheTimeMinutes = e.CacheTimeMinutes,
        DefaultCurrency = e.DefaultCurrency,
        BaggageFeePercent = e.BaggageFeePercent,
        BaggageFeeAmount = e.BaggageFeeAmount,
        CreatedAt = e.CreatedOnUtc,
        ExpiryDate = e.ExpiryDate
    };
}
