using Carter;
using Flight.Application.Features.Admin.Agents;
using Flight.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Shared.Common.Responses;

namespace Flight.Api.Endpoints.Admin.Agents;

public sealed class AdminAgentEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/admin/agents")
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin - Agents");

        grp.MapGet("/", HandleListAsync)
            .WithName("AdminListAgents")
            .WithSummary("Danh sách tất cả agents");

        grp.MapGet("/{id:int}", HandleGetByIdAsync)
            .WithName("AdminGetAgent")
            .WithSummary("Chi tiết agent theo ID");

        grp.MapPost("/", HandleCreateAsync)
            .WithName("AdminCreateAgent")
            .WithSummary("Tạo agent mới");

        grp.MapPut("/{id:int}", HandleUpdateAsync)
            .WithName("AdminUpdateAgent")
            .WithSummary("Cập nhật toàn bộ thông tin agent");

        grp.MapDelete("/{id:int}", HandleDeleteAsync)
            .WithName("AdminDeleteAgent")
            .WithSummary("Xoá mềm agent (soft delete)");

        grp.MapPatch("/{id:int}/active", HandleToggleActiveAsync)
            .WithName("AdminToggleAgentActive")
            .WithSummary("Bật/tắt trạng thái active của agent");

        grp.MapPut("/{id:int}/password", HandleChangePasswordAsync)
            .WithName("AdminChangeAgentPassword")
            .WithSummary("Đổi password hash của agent");
    }

    // ── List ─────────────────────────────────────────────────────────────────
    private static async Task<IResult> HandleListAsync(
        ISender sender,
        int page = 1, int pageSize = 50,
        bool? active = null,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetAgentsQuery(page, pageSize, active), ct);
        return Results.Ok(new ApiResponse<object>(true, "Success", result));
    }

    // ── Get by ID ────────────────────────────────────────────────────────────
    private static async Task<IResult> HandleGetByIdAsync(
        ISender sender,
        int id,
        CancellationToken ct = default)
    {
        var agent = await sender.Send(new GetAgentByIdQuery(id), ct);
        if (agent is null) return Results.NotFound();
        return Results.Ok(new ApiResponse<object>(true, "Success", agent));
    }

    // ── Create ───────────────────────────────────────────────────────────────
    private static async Task<IResult> HandleCreateAsync(
        ISender sender,
        CreateAgentRequest req,
        CancellationToken ct = default)
    {
        try
        {
            var created = await sender.Send(new CreateAgentCommand(
                AgentCode: req.AgentCode,
                Name: req.Name,
                Email: req.Email,
                PasswordHash: req.PasswordHash,
                Address: req.Address,
                Tel: req.Tel,
                LccVnActiveDomestic: req.LccVnActiveDomestic,
                LccVnActiveGlobal: req.LccVnActiveGlobal,
                GalileoPcc: req.GalileoPcc,
                GalileoActive: req.GalileoActive,
                DefaultCurrency: req.DefaultCurrency,
                EnableCache: req.EnableCache,
                CacheTimeMinutes: req.CacheTimeMinutes,
                SendMailInApi: req.SendMailInApi,
                EmailSender: req.EmailSender,
                CombinedMode: req.CombinedMode,
                ExpiryDate: req.ExpiryDate,
                BaggageFeePercent: req.BaggageFeePercent,
                BaggageFeeAmount: req.BaggageFeeAmount), ct);

            return Results.Created(
                $"/api/admin/agents/{created.Id}",
                new ApiResponse<AgentDetailDto>(true, "Created", created));
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new ApiResponse<object>(false, ex.Message, null));
        }
    }

    // ── Update ───────────────────────────────────────────────────────────────
    private static async Task<IResult> HandleUpdateAsync(
        ISender sender,
        int id,
        UpdateAgentRequest req,
        CancellationToken ct = default)
    {
        var updated = await sender.Send(new UpdateAgentCommand(
            Id: id,
            Name: req.Name,
            Email: req.Email,
            Address: req.Address,
            Tel: req.Tel,
            LccVnActiveDomestic: req.LccVnActiveDomestic,
            LccVnActiveGlobal: req.LccVnActiveGlobal,
            GalileoPcc: req.GalileoPcc,
            GalileoActive: req.GalileoActive,
            DefaultCurrency: req.DefaultCurrency,
            EnableCache: req.EnableCache,
            CacheTimeMinutes: req.CacheTimeMinutes,
            SendMailInApi: req.SendMailInApi,
            EmailSender: req.EmailSender,
            CombinedMode: req.CombinedMode,
            ExpiryDate: req.ExpiryDate,
            Active: req.Active,
            BaggageFeePercent: req.BaggageFeePercent,
            BaggageFeeAmount: req.BaggageFeeAmount), ct);

        return updated is null
            ? Results.NotFound()
            : Results.Ok(new ApiResponse<AgentDetailDto>(true, "Updated", updated));
    }

    // ── Delete (soft) ────────────────────────────────────────────────────────
    private static async Task<IResult> HandleDeleteAsync(
        ISender sender,
        int id,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new DeleteAgentCommand(id), ct);
        return result
            ? Results.Ok(new ApiResponse<object>(true, $"Agent {id} deleted.", null))
            : Results.NotFound();
    }

    // ── Toggle Active ────────────────────────────────────────────────────────
    private static async Task<IResult> HandleToggleActiveAsync(
        ISender sender,
        int id,
        bool active,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new ToggleAgentActiveCommand(id, active), ct);
        return result
            ? Results.Ok(new ApiResponse<object>(true, $"Agent {id} active = {active}.", null))
            : Results.NotFound();
    }

    // ── Change Password ──────────────────────────────────────────────────────
    private static async Task<IResult> HandleChangePasswordAsync(
        ISender sender,
        int id,
        ChangePasswordRequest req,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.PasswordHash))
            return Results.BadRequest("PasswordHash is required.");

        var result = await sender.Send(new ChangeAgentPasswordCommand(id, req.PasswordHash), ct);
        return result
            ? Results.Ok(new ApiResponse<object>(true, "Password updated.", null))
            : Results.NotFound();
    }
}

// ── Request DTOs ─────────────────────────────────────────────────────────────

public sealed class CreateAgentRequest
{
    public string AgentCode { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string? Address { get; set; }
    public string? Tel { get; set; }
    public bool LccVnActiveDomestic { get; set; }
    public bool LccVnActiveGlobal { get; set; }
    public string? GalileoPcc { get; set; }
    public bool GalileoActive { get; set; }
    public string? DefaultCurrency { get; set; } = "VND";
    public bool EnableCache { get; set; }
    public int CacheTimeMinutes { get; set; } = 15;
    public bool SendMailInApi { get; set; }
    public int EmailSender { get; set; }
    public int CombinedMode { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal BaggageFeePercent { get; set; }
    public decimal BaggageFeeAmount { get; set; }
}

public sealed class UpdateAgentRequest
{
    public string Name { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? Address { get; set; }
    public string? Tel { get; set; }
    public bool LccVnActiveDomestic { get; set; }
    public bool LccVnActiveGlobal { get; set; }
    public string? GalileoPcc { get; set; }
    public bool GalileoActive { get; set; }
    public string? DefaultCurrency { get; set; } = "VND";
    public bool EnableCache { get; set; }
    public int CacheTimeMinutes { get; set; } = 15;
    public bool SendMailInApi { get; set; }
    public int EmailSender { get; set; }
    public int CombinedMode { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool Active { get; set; } = true;
    public decimal BaggageFeePercent { get; set; }
    public decimal BaggageFeeAmount { get; set; }
}

public sealed class ChangePasswordRequest
{
    public string PasswordHash { get; set; } = default!;
}
