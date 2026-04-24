using Carter;
using Flight.Application.Dtos;
using Flight.Application.Features.Admin.AirlineIgnores;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Shared.Common.Responses;

namespace Flight.Api.Endpoints.Admin.AirlineIgnores;

public sealed class AirlineIgnoreAdminEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/airline-ignores")
            .WithTags("Admin - Airline Ignores")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/", GetAirlineIgnores);
        group.MapGet("/{id:int}", GetAirlineIgnoreById);
        group.MapPost("/", CreateAirlineIgnore);
        group.MapPut("/{id:int}", UpdateAirlineIgnore);
        group.MapDelete("/{id:int}", DeleteAirlineIgnore);
    }

    private static async Task<IResult> GetAirlineIgnores(ISender sender, int? agentId = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetAirlineIgnoresQuery(page, pageSize, agentId), ct);
        return Results.Ok(new ApiResponse<object>(true, "OK", new { total = result.Total, Page = result.Page, PageSize = result.PageSize, Data = result.Data }));
    }

    private static async Task<IResult> GetAirlineIgnoreById(int id, ISender sender, CancellationToken ct = default)
    {
        var item = await sender.Send(new GetAirlineIgnoreByIdQuery(id), ct);
        return item is null ? Results.NotFound() : Results.Ok(new ApiResponse<AirlineIgnoreAdminDto>(true, "OK", item));
    }

    private static async Task<IResult> CreateAirlineIgnore(CreateAirlineIgnoreRequest req, ISender sender, CancellationToken ct = default)
    {
        var created = await sender.Send(new CreateAirlineIgnoreCommand(req.AgentId, req.Airline, req.FilterByPlatingCarrier, req.FilterByAnySegment, req.FilterByAllSegment), ct);
        if (created is null) return Results.BadRequest(new ApiResponse<object>(false, "Already exists", null));
        return Results.Created($"/api/admin/airline-ignores/{created.Id}", new ApiResponse<AirlineIgnoreAdminDto>(true, "Created", created));
    }

    private static async Task<IResult> UpdateAirlineIgnore(int id, CreateAirlineIgnoreRequest req, ISender sender, CancellationToken ct = default)
    {
        var updated = await sender.Send(new UpdateAirlineIgnoreCommand(id, req.FilterByPlatingCarrier, req.FilterByAnySegment, req.FilterByAllSegment), ct);
        return updated is null ? Results.NotFound() : Results.Ok(new ApiResponse<AirlineIgnoreAdminDto>(true, "Updated", updated));
    }

    private static async Task<IResult> DeleteAirlineIgnore(int id, ISender sender, CancellationToken ct = default)
    {
        var success = await sender.Send(new DeleteAirlineIgnoreCommand(id), ct);
        return success ? Results.NoContent() : Results.NotFound();
    }
}