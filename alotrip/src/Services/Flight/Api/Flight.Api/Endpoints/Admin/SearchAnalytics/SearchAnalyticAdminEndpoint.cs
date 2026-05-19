using Carter;
using Flight.Application.Dtos;
using Flight.Application.Features.Admin.SearchAnalytics;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Shared.Common.Responses;

namespace Flight.Api.Endpoints.Admin.SearchAnalytics;

public sealed class SearchAnalyticAdminEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/search-analytics")
            .WithTags("Admin - Search Analytics")
            //.RequireAuthorization("AdminOnly");

        group.MapGet("/", GetSearchAnalytics);
        group.MapGet("/{id:int}", GetSearchAnalyticById);
        group.MapPost("/", CreateSearchAnalytic);
    }

    private static async Task<IResult> GetSearchAnalytics(
        ISender sender,
        string? agentCode = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await sender.Send(
            new GetSearchAnalyticsQuery(agentCode, fromDate, toDate, page, pageSize), ct);
        return Results.Ok(new ApiResponse<object>(true, "OK", new
        {
            total = result.Total, Page = result.Page, PageSize = result.PageSize, Data = result.Data
        }));
    }

    private static async Task<IResult> GetSearchAnalyticById(
        int id, ISender sender, CancellationToken ct = default)
    {
        var item = await sender.Send(new GetSearchAnalyticByIdQuery(id), ct);
        return item is null
            ? Results.NotFound()
            : Results.Ok(new ApiResponse<SearchAnalyticDto>(true, "OK", item));
    }

    private static async Task<IResult> CreateSearchAnalytic(
        CreateSearchAnalyticRequest req, ISender sender, CancellationToken ct = default)
    {
        var created = await sender.Send(new CreateSearchAnalyticCommand(
            req.AgentCode, req.StartPoint, req.EndPoint,
            req.Itinerary, req.DepartDate, req.ReturnDate,
            req.FlightType, req.IPAddress), ct);
        return Results.Created(
            $"/api/admin/search-analytics/{created.Id}",
            new ApiResponse<SearchAnalyticDto>(true, "Created", created));
    }
}
