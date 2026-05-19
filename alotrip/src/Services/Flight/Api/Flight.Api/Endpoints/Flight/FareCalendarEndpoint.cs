using Carter;
using Flight.Application.Features.FareCalendar;
using MediatR;
using Microsoft.AspNetCore.Http;
using Shared.Common.Responses;

namespace Flight.Api.Endpoints.Flight;

/// <summary>
/// Fare calendar endpoints — min fare per day/month.
/// Migrated from old SOAP:
///   GetCacheInMonth     → GET /api/flights/fare-calendar/month
///   GetCacheByListMonth → GET /api/flights/fare-calendar/months
/// </summary>
public sealed class FareCalendarEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/flights/fare-calendar/month", HandleGetMonthAsync)
            .WithName("GetFareCalendarMonth")
            .WithTags("Flights", "FareCalendar")
            .WithSummary("Daily min fares for a month (matches old GetCacheInMonth)")
            .RequireAuthorization("AgentOnly")
            .Produces<ApiResponse<object>>(200)
            .Produces<ProblemDetails>(400);

        app.MapGet("/api/flights/fare-calendar/months", HandleGetMonthsAsync)
            .WithName("GetFareCalendarMonths")
            .WithTags("Flights", "FareCalendar")
            .WithSummary("Cheapest day per month across multiple months (matches old GetCacheByListMonth)")
            .RequireAuthorization("AgentOnly")
            .Produces<ApiResponse<object>>(200)
            .Produces<ProblemDetails>(400);
    }

    /// <summary>
    /// GET /api/flights/fare-calendar/month?origin=SGN&amp;destination=HAN&amp;year=2025&amp;month=6&amp;cacheTimeInMinutes=60
    /// Returns daily min fares for the given month (from today onward).
    /// </summary>
    private static async Task<IResult> HandleGetMonthAsync(
        ISender sender,
        string origin, string destination,
        int year, int month,
        int cacheTimeInMinutes = 1440,
        CancellationToken ct = default)
    {
        var query = new GetFareCalendarMonthQuery(
            origin.ToUpperInvariant(), destination.ToUpperInvariant(),
            year, month, cacheTimeInMinutes);

        var result = await sender.Send(query, ct);
        return Results.Ok(new ApiResponse<object>(true, "Success", result));
    }

    /// <summary>
    /// GET /api/flights/fare-calendar/months?origin=SGN&amp;destination=HAN&amp;months=2025-01,2025-02,2025-03
    /// Returns the single cheapest fare entry per month (cheapest day to fly).
    /// </summary>
    private static async Task<IResult> HandleGetMonthsAsync(
        ISender sender,
        string origin, string destination,
        string months,
        CancellationToken ct = default)
    {
        var monthList = months
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        var query = new GetFareCalendarMonthsQuery(
            origin.ToUpperInvariant(), destination.ToUpperInvariant(), monthList);

        var result = await sender.Send(query, ct);
        return Results.Ok(new ApiResponse<object>(true, "Success", result));
    }
}
