using Flight.Application.Dtos;
using Flight.Application.Interfaces;
using Flight.Domain.Exceptions;
using FluentValidation;
using Shared.BuildingBlocks.CQRS;

namespace Flight.Application.Features.VerifyFare;

// ── Query ────────────────────────────────────────────────────────────────────
public sealed record VerifyFareQuery(string FareId, string SessionData)
    : IQuery<VerifyFareResult>;

public sealed record VerifyFareResult(
    bool    PriceChanged,
    decimal OldPrice,
    decimal NewPrice,
    string  Currency,
    FareDataDto Fare);

// ── Validator ────────────────────────────────────────────────────────────────
public sealed class VerifyFareQueryValidator : AbstractValidator<VerifyFareQuery>
{
    public VerifyFareQueryValidator()
    {
        RuleFor(x => x.FareId).NotEmpty();
        RuleFor(x => x.SessionData).NotEmpty();
    }
}

// ── Handler ──────────────────────────────────────────────────────────────────
public sealed class VerifyFareQueryHandler(
    IEnumerable<IFlightEngine> engines,
    IFlightCacheService cache)
    : IQueryHandler<VerifyFareQuery, VerifyFareResult>
{
    public async Task<VerifyFareResult> Handle(VerifyFareQuery query, CancellationToken ct)
    {
        var cachedFare = await cache.GetFareDataAsync(query.FareId, ct)
            ?? throw new FareExpiredException(query.FareId);

        var engine = engines.FirstOrDefault(e => e.Source == cachedFare.Source && e.IsEnabled)
            ?? throw new DomainException($"Engine '{cachedFare.Source}' is not available.");

        var currentFare = await engine.VerifyFareAsync(query.FareId, query.SessionData, ct)
            ?? throw new FareExpiredException(query.FareId);

        var priceChanged = Math.Abs(currentFare.TotalFare - cachedFare.TotalFare) > 1;

        // Cập nhật cache với giá mới nhất
        await cache.SetFareDataAsync(query.FareId, currentFare, TimeSpan.FromMinutes(30), ct);

        return new VerifyFareResult(priceChanged, cachedFare.TotalFare,
            currentFare.TotalFare, currentFare.Currency, currentFare);
    }
}
