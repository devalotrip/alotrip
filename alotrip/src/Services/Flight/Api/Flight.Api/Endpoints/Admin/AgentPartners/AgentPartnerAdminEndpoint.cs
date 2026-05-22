using Carter;
using Flight.Application.Dtos;
using Flight.Application.Features.Admin.AgentPartners;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Shared.Common.Responses;

namespace Flight.Api.Endpoints.Admin.AgentPartners;

public sealed class AgentPartnerAdminEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/agent-partners")
            .WithTags("Admin - Agent Partners");
            //.RequireAuthorization("AdminOnly");

        group.MapGet("/", GetAgentPartners);
        group.MapPost("/", CreateAgentPartner);
        group.MapDelete("/{id:int}", DeleteAgentPartner);
    }

    private static async Task<IResult> GetAgentPartners(ISender sender, int? agentId = null, int? partnerId = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetAgentPartnersQuery(page, pageSize, agentId, partnerId), ct);
        return Results.Ok(new ApiResponse<object>(true, "OK", new { total = result.Total, Page = result.Page, PageSize = result.PageSize, Data = result.Data }));
    }

    private static async Task<IResult> CreateAgentPartner(CreateAgentPartnerRequest req, ISender sender, CancellationToken ct = default)
    {
        var created = await sender.Send(new CreateAgentPartnerCommand(req.AgentId, req.PartnerId, req.IgnoredMode, req.ListStartPoint), ct);
        return Results.Created($"/api/admin/agent-partners/{created.Id}", new ApiResponse<AgentPartnerDto>(true, "Created", created));
    }

    private static async Task<IResult> DeleteAgentPartner(int id, ISender sender, CancellationToken ct = default)
    {
        var success = await sender.Send(new DeleteAgentPartnerCommand(id), ct);
        return success ? Results.NoContent() : Results.NotFound();
    }
}