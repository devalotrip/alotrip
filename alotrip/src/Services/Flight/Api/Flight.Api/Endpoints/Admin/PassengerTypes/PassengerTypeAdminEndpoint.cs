using Carter;
using Flight.Application.Dtos;
using Flight.Application.Features.Admin.PassengerTypes;
using MediatR;
using Shared.Common.Responses;

namespace Flight.Api.Endpoints.Admin.PassengerTypes;

/// <summary>
/// Admin CRUD for passenger type lookup table (ADT/CHD/INF).
/// Matches old PassengerTypeDB CRUD operations.
/// </summary>
public sealed class PassengerTypeAdminEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/passenger-types")
            .WithTags("Admin - Passenger Types");
		//.RequireAuthorization("AdminOnly");

		group.MapGet("/", GetPassengerTypes);
        group.MapGet("/{code}", GetPassengerTypeByCode);
        group.MapPost("/", CreatePassengerType);
        group.MapPut("/{code}", UpdatePassengerType);
        group.MapDelete("/{code}", DeletePassengerType);
    }

    private static async Task<IResult> GetPassengerTypes(
        ISender sender, int page = 1, int pageSize = 20, string? code = null, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetPassengerTypesQuery(page, pageSize, code), ct);
        return Results.Ok(new ApiResponse<object>(true, "OK", result));
    }

    private static async Task<IResult> GetPassengerTypeByCode(
        ISender sender, string code, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetPassengerTypeByCodeQuery(code), ct);
        return result is null
            ? Results.NotFound(new ApiResponse<object>(false, "Passenger type not found", null))
            : Results.Ok(new ApiResponse<PassengerTypeDto>(true, "OK", result));
    }

    private static async Task<IResult> CreatePassengerType(
        ISender sender, CreatePassengerTypeRequest req, CancellationToken ct = default)
    {
        var result = await sender.Send(
            new CreatePassengerTypeCommand(req.Code, req.Icon, req.NameVi, req.NameEn, req.NameFr, req.Description), ct);
        return Results.Created($"/api/admin/passenger-types/{result.Code}",
            new ApiResponse<PassengerTypeDto>(true, "Created", result));
    }

    private static async Task<IResult> UpdatePassengerType(
        ISender sender, string code, UpdatePassengerTypeRequest req, CancellationToken ct = default)
    {
        var result = await sender.Send(
            new UpdatePassengerTypeCommand(code, req.Icon, req.NameVi, req.NameEn, req.NameFr, req.Description), ct);
        return result is null
            ? Results.NotFound(new ApiResponse<object>(false, "Passenger type not found", null))
            : Results.Ok(new ApiResponse<PassengerTypeDto>(true, "Updated", result));
    }

    private static async Task<IResult> DeletePassengerType(
        ISender sender, string code, CancellationToken ct = default)
    {
        var result = await sender.Send(new DeletePassengerTypeCommand(code), ct);
        return result
            ? Results.NoContent()
            : Results.NotFound(new ApiResponse<object>(false, "Passenger type not found", null));
    }
}
