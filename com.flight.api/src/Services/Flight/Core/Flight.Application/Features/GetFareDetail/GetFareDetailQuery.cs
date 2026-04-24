using Flight.Application.Dtos;
using Flight.Application.Interfaces;
using FluentValidation;
using Shared.BuildingBlocks.CQRS;
using Shared.BuildingBlocks.Exceptions;

namespace Flight.Application.Features.GetFareDetail;

// ── Query ─────────────────────────────────────────────────────────────────────
public sealed record GetFareDetailQuery(string FareId)
    : IQuery<FareDataDto>;

public sealed class GetFareDetailQueryValidator : AbstractValidator<GetFareDetailQuery>
{
    public GetFareDetailQueryValidator()
    {
        RuleFor(x => x.FareId).NotEmpty();
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class GetFareDetailQueryHandler(IFlightCacheService cache)
    : IQueryHandler<GetFareDetailQuery, FareDataDto>
{
    public async Task<FareDataDto> Handle(GetFareDetailQuery query, CancellationToken ct)
    {
        var fareData = await cache.GetFareDataAsync(query.FareId, ct)
            ?? throw new NotFoundException($"Fare '{query.FareId}' not found or expired.");

        return fareData;
    }
}
