using Carter;
using Flight.Application.Dtos;
using Flight.Application.Features.Admin.CarRentals;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Shared.Common.Responses;

namespace Flight.Api.Endpoints.Admin.CarRentals;

public sealed class CarRentalAdminEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/car-rentals")
            .WithTags("Admin - Car Rentals")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/", GetCarRentals);
        group.MapGet("/{id:int}", GetCarRentalById);
        group.MapPost("/", CreateCarRental);
        group.MapDelete("/{id:int}", DeleteCarRental);
    }

    private static async Task<IResult> GetCarRentals(ISender sender, int page = 1, int pageSize = 20, string? bookingId = null, CancellationToken ct = default)
    {
        Guid? parsedBookingId = string.IsNullOrEmpty(bookingId) ? null : Guid.Parse(bookingId);
        var result = await sender.Send(new GetCarRentalsQuery(page, pageSize, parsedBookingId), ct);
        return Results.Ok(new ApiResponse<object>(true, "OK", new { total = result.Total, Page = result.Page, PageSize = result.PageSize, Data = result.Data }));
    }

    private static async Task<IResult> GetCarRentalById(int id, ISender sender, CancellationToken ct = default)
    {
        var item = await sender.Send(new GetCarRentalByIdQuery(id), ct);
        return item is null ? Results.NotFound(new ApiResponse<object>(false, "Car rental not found", null)) : Results.Ok(new ApiResponse<CarRentalDto>(true, "OK", item));
    }

    private static async Task<IResult> CreateCarRental(CreateCarRentalRequest req, ISender sender, CancellationToken ct = default)
    {
        var created = await sender.Send(new CreateCarRentalCommand(req.BookingId, req.Provider, req.PickupLocation, req.DropoffLocation, req.PickupDate, req.DropoffDate, req.CarType, req.CarModel ?? string.Empty, req.DailyRate, req.Currency), ct);
        return Results.Created($"/api/admin/car-rentals/{created.Id}", new ApiResponse<CarRentalDto>(true, "Created", created));
    }

    private static async Task<IResult> DeleteCarRental(int id, ISender sender, CancellationToken ct = default)
    {
        var success = await sender.Send(new DeleteCarRentalCommand(id), ct);
        return success ? Results.NoContent() : Results.NotFound(new ApiResponse<object>(false, "Car rental not found", null));
    }
}