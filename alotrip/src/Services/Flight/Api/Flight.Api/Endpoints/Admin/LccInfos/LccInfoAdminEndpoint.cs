using Carter;
using Flight.Application.Dtos;
using Flight.Application.Features.Admin.LccInfos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Shared.Common.Responses;

namespace Flight.Api.Endpoints.Admin.LccInfos;

public sealed class LccInfoAdminEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/lcc-infos")
            .WithTags("Admin - LCC Infos")
            //.RequireAuthorization("AdminOnly");

        group.MapGet("/", GetLccInfos);
        group.MapGet("/{id:int}", GetLccInfoById);
        group.MapPost("/", CreateLccInfo);
        group.MapPut("/{id:int}", UpdateLccInfo);
        group.MapDelete("/{id:int}", DeleteLccInfo);
    }

    private static async Task<IResult> GetLccInfos(ISender sender, int? agentId = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetLccInfosQuery(agentId, page, pageSize), ct);
        return Results.Ok(new ApiResponse<object>(true, "OK", new { total = result.Total, Page = result.Page, PageSize = result.PageSize, Data = result.Data }));
    }

    private static async Task<IResult> GetLccInfoById(int id, ISender sender, CancellationToken ct = default)
    {
        var item = await sender.Send(new GetLccInfoByIdQuery(id), ct);
        return item is null ? Results.NotFound() : Results.Ok(new ApiResponse<LccInfoDto>(true, "OK", item));
    }

    private static async Task<IResult> CreateLccInfo(CreateLccInfoRequest req, ISender sender, CancellationToken ct = default)
    {
        var created = await sender.Send(new CreateLccInfoCommand(req.AgentId, req.Airline, req.AllowSearch, req.AllowBook), ct);
        if (created is null) return Results.BadRequest(new ApiResponse<object>(false, "Already exists", null));
        return Results.Created($"/api/admin/lcc-infos/{created.Id}", new ApiResponse<LccInfoDto>(true, "Created", created));
    }

    private static async Task<IResult> UpdateLccInfo(int id, UpdateLccInfoRequest req, ISender sender, CancellationToken ct = default)
    {
        var updated = await sender.Send(new UpdateLccInfoCommand(id, req.AllowSearch, req.AllowBook, req.ProxyServerId, req.ProxyServerBookId), ct);
        return updated is null ? Results.NotFound() : Results.Ok(new ApiResponse<LccInfoDto>(true, "Updated", updated));
    }

    private static async Task<IResult> DeleteLccInfo(int id, ISender sender, CancellationToken ct = default)
    {
        var success = await sender.Send(new DeleteLccInfoCommand(id), ct);
        return success ? Results.NoContent() : Results.NotFound();
    }
}