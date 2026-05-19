using Flight.Application.Interfaces;
using MediatR;
using Shared.BuildingBlocks.Exceptions;

namespace Flight.Application.Features.ExchangeCurrency;

// ── Response ──────────────────────────────────────────────────────────────────

public sealed class ExchangeResultDto
{
    public string  From        { get; init; } = default!;
    public string  To          { get; init; } = default!;
    public decimal Amount      { get; init; }
    public decimal Converted   { get; init; }
    public decimal Rate        { get; init; }
}

// ── Query ─────────────────────────────────────────────────────────────────────

/// <summary>
/// GET /api/currencies/exchange — tính toán quy đổi tiền tệ.
/// Đều được qui đổi qua VND làm đơn vị trung gian (như logic cũ).
/// Công thức: amount_in_from_currency / from_rate * to_rate
/// </summary>
public sealed record ExchangeCurrencyQuery(
    string  From,
    string  To,
    decimal Amount = 1m)
    : IRequest<ExchangeResultDto>;

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class ExchangeCurrencyHandler(ICurrencyRepository repo)
    : IRequestHandler<ExchangeCurrencyQuery, ExchangeResultDto>
{
    public async Task<ExchangeResultDto> Handle(ExchangeCurrencyQuery request, CancellationToken ct)
    {
        string fromCode = request.From.ToUpperInvariant();
        string toCode   = request.To.ToUpperInvariant();

        if (fromCode == toCode)
        {
            return new ExchangeResultDto
            {
                From      = fromCode,
                To        = toCode,
                Amount    = request.Amount,
                Converted = request.Amount,
                Rate      = 1m
            };
        }

        // Load both currencies in parallel
        var fromTask = repo.GetByCodeAsync(fromCode, ct);
        var toTask   = repo.GetByCodeAsync(toCode,   ct);
        await Task.WhenAll(fromTask, toTask);

        var fromCur = fromTask.Result
            ?? throw new NotFoundException($"Currency '{fromCode}' not found.");
        var toCur   = toTask.Result
            ?? throw new NotFoundException($"Currency '{toCode}' not found.");

        // Cross-rate via VND (same as old AirlineWS.ConvertCurrency logic)
        // fromCur.Rate = units of fromCode per 1 VND
        // e.g. VND rate=1, USD rate~=0.000040, EUR rate~=0.000037
        decimal rateFromToTo = fromCur.Rate == 0 ? 0 : toCur.Rate / fromCur.Rate;
        decimal converted    = Math.Round(request.Amount * rateFromToTo, 6);

        return new ExchangeResultDto
        {
            From      = fromCode,
            To        = toCode,
            Amount    = request.Amount,
            Converted = converted,
            Rate      = rateFromToTo
        };
    }
}
