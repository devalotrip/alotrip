using Carter;
using Flight.Application.Features.ExchangeCurrency;
using Flight.Application.Features.GetCurrencies;
using Flight.Application.Features.GetCurrencyByCode;
using MediatR;
using Microsoft.AspNetCore.Http;
using Shared.Common.Responses;

namespace Flight.Api.Endpoints.Currency;

/// <summary>
/// Currency endpoints — tỷ giá + quy đổi.
/// Map từ tblCurrency + AirlineWS.ConvertCurrency() cũ → REST:
///   GET /api/currencies             — danh sách tất cả tiền tệ active
///   GET /api/currencies/{code}      — thông tin tiền tệ theo mã
///   GET /api/currencies/exchange    — quy đổi tiền tệ
/// </summary>
public sealed class CurrencyEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        // Note: /exchange literal must be registered before /{code} to avoid shadowing
        app.MapGet("/api/currencies/exchange", HandleExchangeAsync)
            .WithName("ExchangeCurrency")
            .WithTags("Currencies")
            .WithSummary("Quy đổi tiền tệ (dùng tỷ giá trong DB)")
            .RequireAuthorization("AgentOnly")
            .Produces<ApiResponse<object>>(200)
            .Produces<ProblemDetails>(400)
            .Produces<ProblemDetails>(404);

        app.MapGet("/api/currencies", HandleGetAllAsync)
            .WithName("GetCurrencies")
            .WithTags("Currencies")
            .WithSummary("Danh sách tất cả tiền tệ đang active")
            .RequireAuthorization("AgentOnly")
            .Produces<ApiResponse<object>>(200);

        app.MapGet("/api/currencies/{code}", HandleGetByCodeAsync)
            .WithName("GetCurrencyByCode")
            .WithTags("Currencies")
            .WithSummary("Thông tin tiền tệ theo mã (VND, USD, EUR, ...)")
            .RequireAuthorization("AgentOnly")
            .Produces<ApiResponse<object>>(200)
            .Produces<ProblemDetails>(404);
    }

    private static async Task<IResult> HandleGetAllAsync(
        ISender sender,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetCurrenciesQuery(), ct);
        return Results.Ok(new ApiResponse<object>(true, "Success", result));
    }

    private static async Task<IResult> HandleGetByCodeAsync(
        ISender sender,
        string code,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetCurrencyByCodeQuery(code), ct);
        return Results.Ok(new ApiResponse<object>(true, "Success", result));
    }

    private static async Task<IResult> HandleExchangeAsync(
        ISender sender,
        string from,
        string to,
        decimal amount = 1m,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new ExchangeCurrencyQuery(from, to, amount), ct);
        return Results.Ok(new ApiResponse<object>(true, "Success", result));
    }
}
