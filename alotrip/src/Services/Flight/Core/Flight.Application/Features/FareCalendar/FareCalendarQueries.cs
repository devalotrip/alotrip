using Flight.Application.Dtos;
using Flight.Application.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared.BuildingBlocks.CQRS;

namespace Flight.Application.Features.FareCalendar;

// ── Query 1: Daily min fares for one month ─────────────────────────────────────
// Matches old GetCacheInMonth(Month, StartPoint, EndPoint, CacheTimeInMinute)

/// <summary>
/// Returns all cached daily min fares for a given month (today onward).
/// Each entry represents the cheapest fare found for that route on a specific day.
/// CacheTimeInMinutes filters out stale entries (default: 1440 = 24h).
/// </summary>
public sealed record GetFareCalendarMonthQuery(
    string Origin, string Destination,
    int Year, int Month,
    int CacheTimeInMinutes = 1440)
    : IQuery<List<MinFareEntryDto>>;

public sealed class GetFareCalendarMonthValidator : AbstractValidator<GetFareCalendarMonthQuery>
{
    public GetFareCalendarMonthValidator()
    {
        RuleFor(x => x.Origin).NotEmpty().Length(3).WithMessage("Origin must be a 3-letter IATA code.");
        RuleFor(x => x.Destination).NotEmpty().Length(3).WithMessage("Destination must be a 3-letter IATA code.");
        RuleFor(x => x.Year).InclusiveBetween(2020, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
        RuleFor(x => x.CacheTimeInMinutes).GreaterThan(0);
    }
}

public sealed class GetFareCalendarMonthHandler(
    IFlightCacheService cache,
    ILogger<GetFareCalendarMonthHandler> logger)
    : IQueryHandler<GetFareCalendarMonthQuery, List<MinFareEntryDto>>
{
    public async Task<List<MinFareEntryDto>> Handle(
        GetFareCalendarMonthQuery query, CancellationToken ct)
    {
        logger.LogInformation(
            "FareCalendar month query: {Origin}→{Destination} {Year}-{Month:D2}",
            query.Origin, query.Destination, query.Year, query.Month);

        var entries = await cache.GetMinFaresForMonthAsync(
            query.Origin, query.Destination, query.Year, query.Month, ct);

        // Filter stale entries (matches old CacheTimeInMinute check)
        if (query.CacheTimeInMinutes < 1440) // only filter if stricter than 24h default
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-query.CacheTimeInMinutes);
            entries = entries.Where(e => e.SearchedAt >= cutoff).ToList();
        }

        logger.LogInformation(
            "FareCalendar month returned {Count} entries for {Origin}→{Destination} {Year}-{Month:D2}",
            entries.Count, query.Origin, query.Destination, query.Year, query.Month);

        return entries;
    }
}

// ── Query 2: Cheapest day per month across multiple months ──────────────────
// Matches old GetCacheByListMonth(ListMonth, StartPoint, EndPoint)

/// <summary>
/// For each requested month, returns the single cheapest fare entry (cheapest day to fly).
/// Matches old GetCacheByListMonth — monthly fare overview.
/// Months are passed as "yyyy-MM" strings.
/// </summary>
public sealed record GetFareCalendarMonthsQuery(
    string Origin, string Destination,
    List<string> Months)
    : IQuery<List<MinFareEntryDto>>;

public sealed class GetFareCalendarMonthsValidator : AbstractValidator<GetFareCalendarMonthsQuery>
{
    public GetFareCalendarMonthsValidator()
    {
        RuleFor(x => x.Origin).NotEmpty().Length(3).WithMessage("Origin must be a 3-letter IATA code.");
        RuleFor(x => x.Destination).NotEmpty().Length(3).WithMessage("Destination must be a 3-letter IATA code.");
        RuleFor(x => x.Months).NotEmpty().WithMessage("At least one month is required.");
    }
}

public sealed class GetFareCalendarMonthsHandler(
    IFlightCacheService cache,
    ILogger<GetFareCalendarMonthsHandler> logger)
    : IQueryHandler<GetFareCalendarMonthsQuery, List<MinFareEntryDto>>
{
    public async Task<List<MinFareEntryDto>> Handle(
        GetFareCalendarMonthsQuery query, CancellationToken ct)
    {
        logger.LogInformation(
            "FareCalendar months query: {Origin}→{Destination} for {MonthCount} months",
            query.Origin, query.Destination, query.Months.Count);

        var result = new List<MinFareEntryDto>();

        foreach (var monthStr in query.Months)
        {
            if (!DateTime.TryParseExact(monthStr, "yyyy-MM",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var parsed))
            {
                logger.LogWarning("Invalid month format: {Month}, expected yyyy-MM", monthStr);
                continue;
            }

            var entries = await cache.GetMinFaresForMonthAsync(
                query.Origin, query.Destination, parsed.Year, parsed.Month, ct);

            // Return the single cheapest entry for the month (matches old GetMinInMonth ORDER BY MinPrice TOP 1)
            var cheapest = entries.MinBy(e => e.MinPrice);
            if (cheapest is not null)
                result.Add(cheapest);
        }

        logger.LogInformation(
            "FareCalendar months returned {Count} entries for {Origin}→{Destination}",
            result.Count, query.Origin, query.Destination);

        return result;
    }
}
