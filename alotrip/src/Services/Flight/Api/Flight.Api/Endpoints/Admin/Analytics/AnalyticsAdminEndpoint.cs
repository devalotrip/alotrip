using Carter;
using Flight.Application.Features.Admin.Analytics;
using Flight.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Shared.Common.Responses;

namespace Flight.Api.Endpoints.Admin;

public sealed class AnalyticsAdminEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/analytics")
            .WithTags("Admin Analytics")
            //.RequireAuthorization("AdminOnly");

        group.MapGet("/bookings-summary", GetBookingsSummary);
        group.MapGet("/tickets-issued", GetTicketsIssued);
        group.MapGet("/search-details", GetSearchDetails);
    }

    private static async Task<IResult> GetBookingsSummary(
        ISender sender,
        string? agentCode = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetBookingsSummaryQuery(agentCode, fromDate, toDate), ct);
        return Results.Ok(new ApiResponse<BookingsSummaryDto>(true, "OK", result));
    }

    private static async Task<IResult> GetTicketsIssued(
        ISender sender,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetTicketsIssuedQuery(fromDate, toDate), ct);
        return Results.Ok(new ApiResponse<TicketsIssuedDto>(true, "OK", result));
    }

    private static async Task<IResult> GetSearchDetails(
        ISender sender,
        string? agentCode = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetSearchDetailsQuery(agentCode, fromDate, toDate, page, pageSize), ct);
        return Results.Ok(new ApiResponse<SearchDetailsDto>(true, "OK", result));
    }
}