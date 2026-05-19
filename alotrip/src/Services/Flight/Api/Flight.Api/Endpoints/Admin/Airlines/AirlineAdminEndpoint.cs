using Carter;
using Flight.Application.Dtos;
using Flight.Application.Features.Admin.Airlines;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Shared.Common.Responses;

namespace Flight.Api.Endpoints.Admin.Airlines;

public sealed class AirlineAdminEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/airlines")
            .WithTags("Admin - Airlines")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/", GetAirlines);
        group.MapGet("/{id}", GetAirlineById);
        group.MapPost("/", CreateAirline);
        group.MapPut("/{id}", UpdateAirline);
        group.MapDelete("/{id}", DeleteAirline);
    }

    private static async Task<IResult> GetAirlines(
        ISender sender,
        int page = 1,
        int pageSize = 20,
        string? code = null,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetAirlinesQuery(page, pageSize, code), ct);
        return Results.Ok(new ApiResponse<object>(true, "OK", result));
    }

    private static async Task<IResult> GetAirlineById(ISender sender, int id, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetAirlineByIdQuery(id), ct);
        return result is null
            ? Results.NotFound(new ApiResponse<object>(false, "Airline not found", null))
            : Results.Ok(new ApiResponse<AirlineDto>(true, "OK", result));
    }

    private static async Task<IResult> CreateAirline(ISender sender, CreateAirlineRequest req, CancellationToken ct = default)
    {
        var result = await sender.Send(new CreateAirlineCommand(req.Code, req.Name ?? string.Empty, req.Logo), ct);
        return Results.Created($"/api/admin/airlines/{result.Id}", new ApiResponse<AirlineDto>(true, "Created", result));
    }

    private static async Task<IResult> UpdateAirline(ISender sender, int id, CreateAirlineRequest req, CancellationToken ct = default)
    {
        var result = await sender.Send(new UpdateAirlineCommand(id, req.Name ?? string.Empty, req.Logo), ct);
        return result is null
            ? Results.NotFound(new ApiResponse<object>(false, "Airline not found", null))
            : Results.Ok(new ApiResponse<AirlineDto>(true, "Updated", result));
    }

    private static async Task<IResult> DeleteAirline(ISender sender, int id, CancellationToken ct = default)
    {
        var result = await sender.Send(new DeleteAirlineCommand(id), ct);
        return result
            ? Results.NoContent()
            : Results.NotFound(new ApiResponse<object>(false, "Airline not found", null));
    }
}