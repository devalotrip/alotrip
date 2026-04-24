using Flight.Application.Dtos;
using Flight.Application.Interfaces;
using FluentValidation;
using Shared.BuildingBlocks.CQRS;
using Shared.BuildingBlocks.Exceptions;

namespace Flight.Application.Features.GetBaggages;

// ── Query ─────────────────────────────────────────────────────────────────────
public sealed record GetBaggagesQuery(string FareId, string SessionData)
    : IQuery<BaggageInfoDto>;

public sealed class GetBaggagesQueryValidator : AbstractValidator<GetBaggagesQuery>
{
    public GetBaggagesQueryValidator()
    {
        RuleFor(x => x.FareId).NotEmpty();
        RuleFor(x => x.SessionData).NotEmpty();
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class GetBaggagesQueryHandler(
    IEnumerable<IFlightEngine> engines,
    IFlightCacheService cache)
    : IQueryHandler<GetBaggagesQuery, BaggageInfoDto>
{
    public async Task<BaggageInfoDto> Handle(GetBaggagesQuery query, CancellationToken ct)
    {
        var fareData = await cache.GetFareDataAsync(query.FareId, ct)
            ?? throw new NotFoundException($"Fare '{query.FareId}' not found or expired.");

        var engine = engines.FirstOrDefault(e => e.Source == fareData.Source && e.IsEnabled)
            ?? throw new NotFoundException($"Engine '{fareData.Source}' is not available.");

        return await engine.GetBaggagesAsync(fareData, ct);
    }
}
