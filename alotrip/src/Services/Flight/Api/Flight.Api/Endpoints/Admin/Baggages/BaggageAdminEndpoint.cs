using Carter;
using Flight.Application.Dtos;
using Flight.Application.Features.Admin.Baggages;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Shared.Common.Responses;

namespace Flight.Api.Endpoints.Admin.Baggages;

public sealed class BaggageAdminEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/baggages")
            .WithTags("Admin - Baggages")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/", GetBaggages);
        group.MapGet("/{id}", GetBaggageById);
        group.MapPost("/", CreateBaggage);
        group.MapDelete("/{id}", DeleteBaggage);
    }

    private static async Task<IResult> GetBaggages(ISender sender, int page = 1, int pageSize = 20, Guid? bookingId = null, string? flightNumber = null, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetBaggagesQuery(page, pageSize, bookingId, flightNumber), ct);
        return Results.Ok(new ApiResponse<object>(true, "OK", result));
    }

    private static async Task<IResult> GetBaggageById(ISender sender, int id, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetBaggageByIdQuery(id), ct);
        return result is null ? Results.NotFound(new ApiResponse<object>(false, "Baggage not found", null)) : Results.Ok(new ApiResponse<BaggageDto>(true, "OK", result));
    }

    private static async Task<IResult> CreateBaggage(ISender sender, CreateBaggageRequest req, CancellationToken ct = default)
    {
        var result = await sender.Send(new CreateBaggageCommand(req.BaggageCode, req.FlightId, req.FlightNumber, req.PaxId, req.BookingId, req.Weight, req.WeightUnit, req.PieceAllowance, req.CabinClass, req.BaggageType), ct);
        return Results.Created($"/api/admin/baggages/{result.Id}", new ApiResponse<BaggageDto>(true, "Created", result));
    }

    private static async Task<IResult> DeleteBaggage(ISender sender, int id, CancellationToken ct = default)
    {
        var result = await sender.Send(new DeleteBaggageCommand(id), ct);
        return result ? Results.NoContent() : Results.NotFound(new ApiResponse<object>(false, "Baggage not found", null));
    }
}