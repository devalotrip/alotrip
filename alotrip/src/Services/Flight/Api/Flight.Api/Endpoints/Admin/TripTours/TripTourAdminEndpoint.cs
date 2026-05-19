using Carter;
using Flight.Application.Dtos;
using Flight.Application.Features.Admin.TripTours;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Shared.Common.Responses;

namespace Flight.Api.Endpoints.Admin.TripTours;

public sealed class TripTourAdminEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/trips/tours")
            .WithTags("Admin - Trip Tours")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/", GetTripTours);
        group.MapGet("/{id:int}", GetTripTourById);
        group.MapPost("/", CreateTripTour);
        group.MapPut("/{id:int}", UpdateTripTour);
        group.MapDelete("/{id:int}", DeleteTripTour);
    }

    private static async Task<IResult> GetTripTours(ISender sender, int page = 1, int pageSize = 20, string? bookingId = null, CancellationToken ct = default)
    {
        Guid? parsedBookingId = string.IsNullOrEmpty(bookingId) ? null : Guid.Parse(bookingId);
        var result = await sender.Send(new GetTripToursQuery(page, pageSize, parsedBookingId), ct);
        return Results.Ok(new ApiResponse<object>(true, "OK", new { total = result.Total, Page = result.Page, PageSize = result.PageSize, Data = result.Data }));
    }

    private static async Task<IResult> GetTripTourById(int id, ISender sender, CancellationToken ct = default)
    {
        var tripTour = await sender.Send(new GetTripTourByIdQuery(id), ct);
        return tripTour is null ? Results.NotFound(new ApiResponse<object>(false, "Trip Tour not found", null)) : Results.Ok(new ApiResponse<TripTourDto>(true, "OK", tripTour));
    }

    private static async Task<IResult> CreateTripTour(CreateTripTourRequest req, ISender sender, CancellationToken ct = default)
    {
        var created = await sender.Send(new CreateTripTourCommand(req.BookingId, req.TourName, req.TourCode, req.DepartureDate, req.ReturnDate, req.Destination, req.PaxCount, req.UnitPrice, req.TotalAmount, req.Currency, req.Status), ct);
        return Results.Created($"/api/admin/trips/tours/{created.Id}", new ApiResponse<TripTourDto>(true, "Created", created));
    }

    private static async Task<IResult> UpdateTripTour(int id, CreateTripTourRequest req, ISender sender, CancellationToken ct = default)
    {
        var updated = await sender.Send(new UpdateTripTourCommand(id, req.TourName, req.TourCode, req.DepartureDate, req.ReturnDate, req.Destination, req.PaxCount, req.UnitPrice, req.TotalAmount, req.Currency, req.Status), ct);
        return updated is null ? Results.NotFound(new ApiResponse<object>(false, "Trip Tour not found", null)) : Results.Ok(new ApiResponse<TripTourDto>(true, "Updated", updated));
    }

    private static async Task<IResult> DeleteTripTour(int id, ISender sender, CancellationToken ct = default)
    {
        var success = await sender.Send(new DeleteTripTourCommand(id), ct);
        return success ? Results.NoContent() : Results.NotFound(new ApiResponse<object>(false, "Trip Tour not found", null));
    }
}