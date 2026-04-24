using Flight.Application.Dtos;
using Flight.Application.Interfaces;
using FluentValidation;
using Shared.BuildingBlocks.CQRS;
using Shared.BuildingBlocks.Exceptions;

namespace Flight.Application.Features.GetFareRules;

// ── Query ─────────────────────────────────────────────────────────────────────
/// <param name="Itinerary">0 = outbound, 1 = return</param>
public sealed record GetFareRulesQuery(string FareId, string SessionData, int Itinerary = 0)
    : IQuery<List<FareRuleGroupDto>>;

public sealed class GetFareRulesQueryValidator : AbstractValidator<GetFareRulesQuery>
{
    public GetFareRulesQueryValidator()
    {
        RuleFor(x => x.FareId).NotEmpty();
        RuleFor(x => x.SessionData).NotEmpty();
        RuleFor(x => x.Itinerary).InclusiveBetween(0, 1);
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class GetFareRulesQueryHandler(
    IEnumerable<IFlightEngine> engines,
    IFlightCacheService cache)
    : IQueryHandler<GetFareRulesQuery, List<FareRuleGroupDto>>
{
    public async Task<List<FareRuleGroupDto>> Handle(GetFareRulesQuery query, CancellationToken ct)
    {
        var fareData = await cache.GetFareDataAsync(query.FareId, ct)
            ?? throw new NotFoundException($"Fare '{query.FareId}' not found or expired.");

        var engine = engines.FirstOrDefault(e => e.Source == fareData.Source && e.IsEnabled)
            ?? throw new NotFoundException($"Engine '{fareData.Source}' is not available.");

        return await engine.GetFareRulesAsync(fareData, query.Itinerary, ct);
    }
}
