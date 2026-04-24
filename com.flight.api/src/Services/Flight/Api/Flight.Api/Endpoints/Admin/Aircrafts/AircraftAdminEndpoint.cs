using Carter;
using Flight.Application.Dtos;
using Flight.Application.Features.Admin.Aircrafts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Shared.Common.Responses;

namespace Flight.Api.Endpoints.Admin.Aircrafts;

public sealed class AircraftAdminEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/aircrafts")
            .WithTags("Admin - Aircrafts")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/", GetAircrafts);
        group.MapGet("/{id}", GetAircraftById);
        group.MapPost("/", CreateAircraft);
        group.MapPut("/{id}", UpdateAircraft);
        group.MapDelete("/{id}", DeleteAircraft);
    }

    private static async Task<IResult> GetAircrafts(
        ISender sender,
        int page = 1,
        int pageSize = 20,
        string? iata = null,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetAircraftsQuery(page, pageSize, iata), ct);
        return Results.Ok(new ApiResponse<object>(true, "OK", result));
    }

    private static async Task<IResult> GetAircraftById(ISender sender, int id, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetAircraftByIdQuery(id), ct);
        return result is null
            ? Results.NotFound(new ApiResponse<object>(false, "Aircraft not found", null))
            : Results.Ok(new ApiResponse<AircraftDto>(true, "OK", result));
    }

    private static async Task<IResult> CreateAircraft(ISender sender, CreateAircraftRequest req, CancellationToken ct = default)
    {
        var result = await sender.Send(new CreateAircraftCommand(req.IATA, req.Manufacturer ?? string.Empty, req.Model ?? string.Empty), ct);
        return Results.Created($"/api/admin/aircrafts/{result.Id}", new ApiResponse<AircraftDto>(true, "Created", result));
    }

    private static async Task<IResult> UpdateAircraft(ISender sender, int id, CreateAircraftRequest req, CancellationToken ct = default)
    {
        var result = await sender.Send(new UpdateAircraftCommand(id, req.Manufacturer ?? string.Empty, req.Model ?? string.Empty), ct);
        return result is null
            ? Results.NotFound(new ApiResponse<object>(false, "Aircraft not found", null))
            : Results.Ok(new ApiResponse<AircraftDto>(true, "Updated", result));
    }

    private static async Task<IResult> DeleteAircraft(ISender sender, int id, CancellationToken ct = default)
    {
        var result = await sender.Send(new DeleteAircraftCommand(id), ct);
        return result
            ? Results.NoContent()
            : Results.NotFound(new ApiResponse<object>(false, "Aircraft not found", null));
    }
}