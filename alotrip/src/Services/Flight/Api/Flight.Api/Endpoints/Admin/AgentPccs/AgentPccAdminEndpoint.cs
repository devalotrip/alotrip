using Carter;
using Flight.Application.Dtos;
using Flight.Application.Features.Admin.AgentPccs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Shared.Common.Responses;

namespace Flight.Api.Endpoints.Admin.AgentPccs;

public sealed class AgentPccAdminEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/agent-pccs")
            .WithTags("Admin - Agent PCCs")
            //.RequireAuthorization("AdminOnly");

        group.MapGet("/", GetAgentPccs);
        group.MapGet("/{id:int}", GetAgentPccById);
        group.MapPost("/", CreateAgentPcc);
        group.MapPut("/{id:int}", UpdateAgentPcc);
        group.MapDelete("/{id:int}", DeleteAgentPcc);
    }

    private static async Task<IResult> GetAgentPccs(ISender sender, int? agentId = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetAgentPccsQuery(agentId, page, pageSize), ct);
        return Results.Ok(new ApiResponse<object>(true, "OK", new { total = result.Total, Page = result.Page, PageSize = result.PageSize, Data = result.Data }));
    }

    private static async Task<IResult> GetAgentPccById(int id, ISender sender, CancellationToken ct = default)
    {
        var item = await sender.Send(new GetAgentPccByIdQuery(id), ct);
        return item is null ? Results.NotFound() : Results.Ok(new ApiResponse<AgentPccDto>(true, "OK", item));
    }

    private static async Task<IResult> CreateAgentPcc(CreateAgentPccRequest req, ISender sender, CancellationToken ct = default)
    {
        var created = await sender.Send(new CreateAgentPccCommand(req.AgentId, req.Pcc, req.IgnoredMode, req.ListStartPoint), ct);
        if (created is null) return Results.BadRequest(new ApiResponse<object>(false, "Already exists", null));
        return Results.Created($"/api/admin/agent-pccs/{created.Id}", new ApiResponse<AgentPccDto>(true, "Created", created));
    }

    private static async Task<IResult> UpdateAgentPcc(int id, CreateAgentPccRequest req, ISender sender, CancellationToken ct = default)
    {
        var updated = await sender.Send(new UpdateAgentPccCommand(id, req.IgnoredMode, req.ListStartPoint), ct);
        return updated is null ? Results.NotFound() : Results.Ok(new ApiResponse<AgentPccDto>(true, "Updated", updated));
    }

    private static async Task<IResult> DeleteAgentPcc(int id, ISender sender, CancellationToken ct = default)
    {
        var success = await sender.Send(new DeleteAgentPccCommand(id), ct);
        return success ? Results.NoContent() : Results.NotFound();
    }
}