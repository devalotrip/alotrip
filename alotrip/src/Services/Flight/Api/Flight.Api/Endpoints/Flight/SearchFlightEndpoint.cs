using Carter;
using Flight.Application.Features.GetBaggages;
using Flight.Application.Features.GetFareDetail;
using Flight.Application.Features.GetFareRules;
using Flight.Application.Features.SearchFlight;
using Flight.Application.Features.VerifyFare;
using MediatR;
using Microsoft.AspNetCore.Http;
using Shared.BuildingBlocks.Authentication.Extensions;
using Shared.Common.Responses;

namespace Flight.Api.Endpoints.Flight;

/// <summary>
/// Endpoints tìm kiếm và tra cứu chuyến bay.
/// Map từ AirlineWS SOAP methods → REST:
///   SearchFlight   → GET /api/flights/search
///   VerifyFare     → GET /api/flights/{fareId}/verify
///   GetFareDetail  → GET /api/flights/{fareId}
///   GetBaggages    → GET /api/flights/{fareId}/baggages
///   GetFareRules   → GET /api/flights/{fareId}/rules
/// </summary>
public sealed class SearchFlightEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/flights/search", HandleSearchAsync)
            .WithName("SearchFlight")
            .WithTags("Flights")
            .WithSummary("Tìm kiếm chuyến bay (fan-out đến tất cả engines song song)")
            .RequireAuthorization("AgentOnly")
            .Produces<ApiResponse<object>>(200)
            .Produces<ProblemDetails>(400)
            .Produces<ProblemDetails>(401);

        app.MapGet("/api/flights/{fareId}/verify", HandleVerifyAsync)
            .WithName("VerifyFare")
            .WithTags("Flights")
            .WithSummary("Kiểm tra giá vé trước khi đặt (re-validate price + seats)")
            .RequireAuthorization("AgentOnly")
            .Produces<ApiResponse<object>>(200)
            .Produces<ProblemDetails>(401)
            .Produces<ProblemDetails>(410);

        app.MapGet("/api/flights/{fareId}", HandleGetFareDetailAsync)
            .WithName("GetFareDetail")
            .WithTags("Flights")
            .WithSummary("Lấy thông tin chi tiết giá vé đã cache trong Redis")
            .RequireAuthorization("AgentOnly")
            .Produces<ApiResponse<object>>(200)
            .Produces<ProblemDetails>(401)
            .Produces<ProblemDetails>(404);

        app.MapGet("/api/flights/{fareId}/baggages", HandleGetBaggagesAsync)
            .WithName("GetBaggages")
            .WithTags("Flights")
            .WithSummary("Lấy danh sách hành lý mua thêm cho chuyến bay")
            .RequireAuthorization("AgentOnly")
            .Produces<ApiResponse<object>>(200)
            .Produces<ProblemDetails>(401)
            .Produces<ProblemDetails>(404);

        app.MapGet("/api/flights/{fareId}/rules", HandleGetFareRulesAsync)
            .WithName("GetFareRules")
            .WithTags("Flights")
            .WithSummary("Lấy điều kiện vé (hoàn, đổi, quy định hành lý)")
            .RequireAuthorization("AgentOnly")
            .Produces<ApiResponse<object>>(200)
            .Produces<ProblemDetails>(401)
            .Produces<ProblemDetails>(404);
    }

    private static async Task<IResult> HandleSearchAsync(
        ISender sender,
        IHttpContextAccessor httpContextAccessor,
        string from, string to,
        DateTime date,
        DateTime? returnDate = null,
        int adt = 1, int chd = 0, int inf = 0,
        string currency = "VND",
        CancellationToken ct = default)
    {
        // AgentCode is taken from the JWT claim, not from query string
        var agentCode = httpContextAccessor.GetCurrentUser().UserName;

        // Extract client IP for search analytics tracking
        var ipAddress = httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

        var query = new SearchFlightQuery(from, to, date, returnDate, adt, chd, inf, currency, agentCode, ipAddress);
        var result = await sender.Send(query, ct);
        return Results.Ok(new ApiResponse<object>(true, "Success", result));
    }

    private static async Task<IResult> HandleVerifyAsync(
        ISender sender,
        string fareId,
        string sessionData,
        CancellationToken ct = default)
    {
        var query = new VerifyFareQuery(fareId, sessionData);
        var result = await sender.Send(query, ct);
        return Results.Ok(new ApiResponse<object>(true, "Success", result));
    }

    private static async Task<IResult> HandleGetFareDetailAsync(
        ISender sender,
        string fareId,
        CancellationToken ct = default)
    {
        var query = new GetFareDetailQuery(fareId);
        var result = await sender.Send(query, ct);
        return Results.Ok(new ApiResponse<object>(true, "Success", result));
    }

    private static async Task<IResult> HandleGetBaggagesAsync(
        ISender sender,
        string fareId,
        string? sessionData = null,
        CancellationToken ct = default)
    {
        var query = new GetBaggagesQuery(fareId, sessionData ?? string.Empty);
        var result = await sender.Send(query, ct);
        return Results.Ok(new ApiResponse<object>(true, "Success", result));
    }

    private static async Task<IResult> HandleGetFareRulesAsync(
        ISender sender,
        string fareId,
        string? sessionData = null,
        int itinerary = 0,
        CancellationToken ct = default)
    {
        var query = new GetFareRulesQuery(fareId, sessionData ?? string.Empty, itinerary);
        var result = await sender.Send(query, ct);
        return Results.Ok(new ApiResponse<object>(true, "Success", result));
    }
}
