using Carter;
using Flight.Application.Dtos;
using Flight.Application.Features.Admin.TripVisas;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Shared.Common.Responses;

namespace Flight.Api.Endpoints.Admin.TripVisas;

public sealed class TripVisaAdminEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/trips/visas")
            .WithTags("Admin - Trip Visas")
            //.RequireAuthorization("AdminOnly");

        group.MapGet("/", GetTripVisas);
        group.MapGet("/{id:int}", GetTripVisaById);
        group.MapPost("/", CreateTripVisa);
        group.MapDelete("/{id:int}", DeleteTripVisa);
    }

    private static async Task<IResult> GetTripVisas(ISender sender, int page = 1, int pageSize = 20, Guid? bookingId = null, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetTripVisasQuery(page, pageSize, bookingId), ct);
        return Results.Ok(new ApiResponse<object>(true, "OK", new { total = result.Total, Page = result.Page, PageSize = result.PageSize, Data = result.Data }));
    }

    private static async Task<IResult> GetTripVisaById(int id, ISender sender, CancellationToken ct = default)
    {
        var tripVisa = await sender.Send(new GetTripVisaByIdQuery(id), ct);
        return tripVisa is null ? Results.NotFound(new ApiResponse<object>(false, "Trip Visa not found", null)) : Results.Ok(new ApiResponse<TripVisaDto>(true, "OK", tripVisa));
    }

    private static async Task<IResult> CreateTripVisa(CreateTripVisaRequest req, ISender sender, CancellationToken ct = default)
    {
        var created = await sender.Send(new CreateTripVisaCommand(req.BookingId, req.Code, req.Name, req.Price, req.Value, req.Currency, req.PriceVn), ct);
        return Results.Created($"/api/admin/trips/visas/{created.Id}", new ApiResponse<TripVisaDto>(true, "Created", created));
    }

    private static async Task<IResult> DeleteTripVisa(int id, ISender sender, CancellationToken ct = default)
    {
        var success = await sender.Send(new DeleteTripVisaCommand(id), ct);
        return success ? Results.NoContent() : Results.NotFound(new ApiResponse<object>(false, "Trip Visa not found", null));
    }
}