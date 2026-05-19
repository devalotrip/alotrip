using Carter;
using Flight.Application.Dtos;
using Flight.Application.Features.Admin.TripCancellations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Shared.Common.Responses;

namespace Flight.Api.Endpoints.Admin.TripCancellations;

public sealed class TripCancellationAdminEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/trips/cancellations")
            .WithTags("Admin - Trip Cancellations")
            //.RequireAuthorization("AdminOnly");

        group.MapGet("/", GetTripCancellations);
        group.MapGet("/{id:int}", GetTripCancellationById);
        group.MapPost("/", CreateTripCancellation);
        group.MapDelete("/{id:int}", DeleteTripCancellation);
    }

    private static async Task<IResult> GetTripCancellations(ISender sender, int page = 1, int pageSize = 20, Guid? bookingId = null, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetTripCancellationsQuery(page, pageSize, bookingId), ct);
        return Results.Ok(new ApiResponse<object>(true, "OK", new { total = result.Total, Page = result.Page, PageSize = result.PageSize, Data = result.Data }));
    }

    private static async Task<IResult> GetTripCancellationById(int id, ISender sender, CancellationToken ct = default)
    {
        var item = await sender.Send(new GetTripCancellationByIdQuery(id), ct);
        return item is null ? Results.NotFound(new ApiResponse<object>(false, "Trip Cancellation not found", null)) : Results.Ok(new ApiResponse<TripCancellationDto>(true, "OK", item));
    }

    private static async Task<IResult> CreateTripCancellation(CreateTripCancellationRequest req, ISender sender, CancellationToken ct = default)
    {
        var created = await sender.Send(new CreateTripCancellationCommand(req.BookingId, req.MarkupAmount, req.MarkupPercent, req.Price, req.Currency, req.BookingPrice, req.Value), ct);
        return Results.Created($"/api/admin/trips/cancellations/{created.Id}", new ApiResponse<TripCancellationDto>(true, "Created", created));
    }

    private static async Task<IResult> DeleteTripCancellation(int id, ISender sender, CancellationToken ct = default)
    {
        var success = await sender.Send(new DeleteTripCancellationCommand(id), ct);
        return success ? Results.NoContent() : Results.NotFound(new ApiResponse<object>(false, "Trip Cancellation not found", null));
    }
}