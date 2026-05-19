using Carter;
using Flight.Application.Features.Admin.Currencies;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Shared.Common.Responses;

namespace Flight.Api.Endpoints.Admin.Currencies;

public sealed class AdminCurrencyEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/admin/currencies")
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin - Currencies");

        grp.MapGet("/", HandleListAllAsync)
            .WithName("AdminListCurrencies")
            .WithSummary("Danh sách tất cả currencies (kể cả inactive)");

        grp.MapPut("/{code}", HandleUpdateAsync)
            .WithName("AdminUpdateCurrency")
            .WithSummary("Cập nhật currency (rate, name, symbol, round_unit, locked, active)");

        grp.MapPost("/", HandleCreateAsync)
            .WithName("AdminCreateCurrency")
            .WithSummary("Tạo mới currency");

        grp.MapPost("/sync", HandleSyncAsync)
            .WithName("AdminSyncCurrency")
            .WithSummary("Đồng bộ tỷ giá từ API bên ngoài");
    }

    private static async Task<IResult> HandleListAllAsync(ISender sender, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetCurrenciesQuery(), ct);
        return Results.Ok(new ApiResponse<object>(true, "Success", result));
    }

    private static async Task<IResult> HandleUpdateAsync(ISender sender, string code, CurrencyUpdateRequest req, CancellationToken ct = default)
    {
        var result = await sender.Send(new UpdateCurrencyCommand(code, req.Name, req.Rate, req.Symbol, req.RoundUnit, req.Locked, req.Active), ct);
        return result ? Results.Ok(new ApiResponse<object>(true, "Updated.", null)) : Results.NotFound();
    }

    private static async Task<IResult> HandleCreateAsync(ISender sender, CurrencyCreateRequest req, CancellationToken ct = default)
    {
        var result = await sender.Send(new CreateCurrencyCommand(req.Code, req.Name, req.Rate, req.Symbol, req.RoundUnit, req.Locked, req.Active), ct);
        return result ? Results.Created($"/api/currencies/{req.Code}", new ApiResponse<object>(true, "Created.", null)) : Results.BadRequest();
    }

    private static async Task<IResult> HandleSyncAsync(ISender sender, HttpContext context, CancellationToken ct = default)
    {
        var secretKey = context.Request.Headers["X-Secret-Key"].FirstOrDefault();
        var result = await sender.Send(new SyncCurrenciesCommand(secretKey ?? ""), ct);
        return result.Success
            ? Results.Ok(new ApiResponse<object>(true, result.Message, null))
            : Results.Unauthorized();
    }
}

public sealed class CurrencyUpdateRequest
{
    public string? Name { get; set; }
    public decimal? Rate { get; set; }
    public string? Symbol { get; set; }
    public decimal? RoundUnit { get; set; }
    public bool? Locked { get; set; }
    public bool? Active { get; set; }
}

public sealed class CurrencyCreateRequest
{
    public string Code { get; set; } = default!;
    public string? Name { get; set; }
    public decimal Rate { get; set; } = 1m;
    public string? Symbol { get; set; }
    public decimal RoundUnit { get; set; } = 1000m;
    public bool Locked { get; set; }
    public bool Active { get; set; } = true;
}
