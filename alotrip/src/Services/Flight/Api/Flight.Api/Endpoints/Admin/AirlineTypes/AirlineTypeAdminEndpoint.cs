using Carter;
using Flight.Application.Dtos;
using Flight.Application.Features.Admin.AirlineTypes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Shared.Common.Responses;

namespace Flight.Api.Endpoints.Admin.AirlineTypes;

public sealed class AirlineTypeAdminEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/airline-types")
            .WithTags("Admin - Airline Types")
            //.RequireAuthorization("AdminOnly");

        group.MapGet("/", GetAirlineTypes);
        group.MapGet("/{id}", GetAirlineTypeById);
        group.MapPost("/", CreateAirlineType);
        group.MapPut("/{id}", UpdateAirlineType);
        group.MapDelete("/{id}", DeleteAirlineType);
    }

    private static async Task<IResult> GetAirlineTypes(ISender sender, int page = 1, int pageSize = 20, string? code = null, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetAirlineTypesQuery(page, pageSize, code), ct);
        return Results.Ok(new ApiResponse<object>(true, "OK", result));
    }

    private static async Task<IResult> GetAirlineTypeById(ISender sender, int id, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetAirlineTypeByIdQuery(id), ct);
        return result is null ? Results.NotFound(new ApiResponse<object>(false, "Airline Type not found", null)) : Results.Ok(new ApiResponse<AirlineTypeDto>(true, "OK", result));
    }

    private static async Task<IResult> CreateAirlineType(ISender sender, CreateAirlineTypeRequest req, CancellationToken ct = default)
    {
        var result = await sender.Send(new CreateAirlineTypeCommand(req.Code, req.Name ?? string.Empty, req.Description), ct);
        return Results.Created($"/api/admin/airline-types/{result.Id}", new ApiResponse<AirlineTypeDto>(true, "Created", result));
    }

    private static async Task<IResult> UpdateAirlineType(ISender sender, int id, CreateAirlineTypeRequest req, CancellationToken ct = default)
    {
        var result = await sender.Send(new UpdateAirlineTypeCommand(id, req.Name ?? string.Empty, req.Description), ct);
        return result is null ? Results.NotFound(new ApiResponse<object>(false, "Airline Type not found", null)) : Results.Ok(new ApiResponse<AirlineTypeDto>(true, "Updated", result));
    }

    private static async Task<IResult> DeleteAirlineType(ISender sender, int id, CancellationToken ct = default)
    {
        var result = await sender.Send(new DeleteAirlineTypeCommand(id), ct);
        return result ? Results.NoContent() : Results.NotFound(new ApiResponse<object>(false, "Airline Type not found", null));
    }
}