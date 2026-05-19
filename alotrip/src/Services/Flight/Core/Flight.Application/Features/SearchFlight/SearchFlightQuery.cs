using Flight.Application.Dtos;
using Flight.Application.Features.ApplyCommission;
using Flight.Application.Interfaces;
using Flight.Domain.Aggregates.Booking;
using Flight.Domain.Enums;
using Flight.Domain.Repositories;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.BuildingBlocks.CQRS;

namespace Flight.Application.Features.SearchFlight;
public sealed record SearchFlightQuery(
    string Origin, string Destination,
    DateTime DepartDate, DateTime? ReturnDate,
    int AdultCount, int ChildCount, int InfantCount,
    string Currency = "VND", string? AgentCode = null,
    string? IpAddress = null)
    : IQuery<IEnumerable<FareDataDto>>;

// ── Validator ────────────────────────────────────────────────────────────────
public sealed class SearchFlightQueryValidator : AbstractValidator<SearchFlightQuery>
{
    public SearchFlightQueryValidator()
    {
        RuleFor(x => x.Origin)
            .NotEmpty().WithMessage("Origin airport is required.")
            .Length(3).WithMessage("Origin must be a 3-letter IATA code.");

        RuleFor(x => x.Destination)
            .NotEmpty().WithMessage("Destination airport is required.")
            .Length(3).WithMessage("Destination must be a 3-letter IATA code.");

        RuleFor(x => x.Origin)
            .NotEqual(x => x.Destination).WithMessage("Origin and destination cannot be the same.");

        RuleFor(x => x.DepartDate)
            .GreaterThanOrEqualTo(DateTime.Today).WithMessage("Departure date cannot be in the past.");

        RuleFor(x => x.AdultCount)
            .GreaterThanOrEqualTo(1).WithMessage("At least 1 adult is required.");

        RuleFor(x => x.InfantCount)
            .LessThanOrEqualTo(x => x.AdultCount).WithMessage("Infants cannot exceed adult count.");

        RuleFor(x => x.Currency)
            .NotEmpty().Length(3).WithMessage("Currency must be a 3-letter ISO code.");
    }
}

// ── Handler ──────────────────────────────────────────────────────────────────
public sealed class SearchFlightQueryHandler(
    IEnumerable<IFlightEngine> engines,
    IFlightCacheService        cache,
    ISender                    sender,
    IAgentConfigRepository     agentConfigRepository,
    ISearchAnalyticRepository   searchAnalyticRepository,
    IGeoAirportRepository      geoAirportRepository,
    ILogger<SearchFlightQueryHandler> logger)
    : IQueryHandler<SearchFlightQuery, IEnumerable<FareDataDto>>
{
    public async Task<IEnumerable<FareDataDto>> Handle(
        SearchFlightQuery query, CancellationToken ct)
    {
        // 1. Build cache key
        var cacheKey = BuildCacheKey(query);

        // 2. Check Redis cache trước
        var cached = await cache.GetSearchResultAsync(cacheKey, ct);
        if (cached != null)
        {
            logger.LogInformation("Cache HIT for route {Origin}→{Destination} on {Date}",
                query.Origin, query.Destination, query.DepartDate.ToString("yyyy-MM-dd"));

            // Track analytics even on cache hits (every search counts)
            await TrackSearchAnalyticAsync(query, null, "CACHE", ct);
            return cached;
        }

        // 3. Load agent config (controls which engines are allowed)
        AgentConfigDto? agentConfig = null;
        if (!string.IsNullOrWhiteSpace(query.AgentCode))
            agentConfig = await agentConfigRepository.GetByCodeAsync(query.AgentCode, ct);

        // 4. Fan-out song song đến các engines đã được agent cho phép
        var activeEngines = engines
            .Where(e => e.IsEnabled
                && IsEngineAllowed(e, agentConfig)
                && IsRouteAllowedForEngine(e, agentConfig, query.Origin, query.Destination))
            .ToList();

        logger.LogInformation("Searching {EngineCount} engines for {Origin}→{Destination}",
            activeEngines.Count, query.Origin, query.Destination);

        var request = new SearchFlightRequest
        {
            Origin      = query.Origin,
            Destination = query.Destination,
            DepartDate  = query.DepartDate,
            ReturnDate  = query.ReturnDate,
            AdultCount  = query.AdultCount,
            ChildCount  = query.ChildCount,
            InfantCount = query.InfantCount,
            Currency    = query.Currency,
            AgentCode   = query.AgentCode
        };

        var tasks = activeEngines.Select(e => SafeSearchAsync(e, request, ct));
        var results = await Task.WhenAll(tasks);

        var fares = results
            .SelectMany(r => r)
            .OrderBy(f => f.TotalFare)
            .ToList();

        logger.LogInformation("Found {FareCount} raw fares for {Origin}→{Destination}",
            fares.Count, query.Origin, query.Destination);

        // 5. Apply airline ignore filters
        if (agentConfig?.AirlineIgnores.Count > 0)
            fares = ApplyAirlineIgnores(fares, agentConfig.AirlineIgnores);

        // 6. Apply agent commission / service fee
        if (fares.Count > 0 && !string.IsNullOrWhiteSpace(query.AgentCode))
        {
            var applied = await sender.Send(new ApplyCommissionCommand(fares, query.AgentCode), ct);
            fares = applied.ToList();
        }

        // Re-sort after commission adjustment changes TotalFare
        fares.Sort((a, b) => a.TotalFare.CompareTo(b.TotalFare));

        // 7. Lưu vào cache 15 phút
        if (fares.Count > 0)
        {
            await cache.SetSearchResultAsync(cacheKey, fares, TimeSpan.FromMinutes(15), ct);

            // 7b. Save min fare for calendar (passive cache, matches old SaveForCache)
            await SaveMinFareForCalendarAsync(fares, ct);
        }

        // 8. Track search analytics (fire-inline, best-effort)
        var sourcesStr = string.Join(",", activeEngines.Select(e => e.Source.ToString()));
        await TrackSearchAnalyticAsync(query, agentConfig, sourcesStr, ct);

        return fares;
    }

    // ── Min fare calendar population ─────────────────────────────────────────

    /// <summary>
    /// After every search, saves the cheapest fare as a min-fare calendar entry.
    /// Matches old SaveForCache() from Interface.cs — passive cache populated by real searches.
    /// Only saves if the new price is cheaper than the existing cached entry (or entry is missing).
    /// </summary>
    private async Task SaveMinFareForCalendarAsync(List<FareDataDto> fares, CancellationToken ct)
    {
        try
        {
            var cheapest = fares[0]; // already sorted by TotalFare ascending

            // Check if an existing entry is already cheaper (avoid overwriting better data)
            var existing = await cache.GetMinFareEntryAsync(
                cheapest.Origin, cheapest.Destination, cheapest.DepartDate, ct);
            if (existing is not null && existing.MinPrice <= cheapest.AdultFare)
                return;

            var entry = new MinFareEntryDto
            {
                Origin        = cheapest.Origin,
                Destination   = cheapest.Destination,
                DepartDate    = cheapest.DepartDate,
                Airline       = cheapest.Airline,
                MinPrice      = cheapest.AdultFare,
                ServiceFee    = cheapest.ServiceFee,
                Currency      = cheapest.Currency,
                ItineraryType = cheapest.TripType == TripType.RoundTrip ? 2 : 1,
                ReturnDate    = cheapest.ReturnDate,
                SearchedAt    = DateTime.UtcNow,
            };
            await cache.SetMinFareEntryAsync(entry, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to save min fare for calendar");
        }
    }

    // ── Search analytics tracking ─────────────────────────────────────────────

    /// <summary>
    /// Persists a search analytic record. Matches old IncrementingSearch() from SearchAnalyticDB.
    /// Also records which engines were queried (Sources) — maps to old tblSearchDetail.System concept.
    /// Best-effort: failures are logged but never block the search response.
    /// </summary>
    private async Task TrackSearchAnalyticAsync(
        SearchFlightQuery query, AgentConfigDto? agentConfig, string sources, CancellationToken ct)
    {
        try
        {
            // Itinerary: 1 = one-way, 2 = round-trip (matches old enum)
            var itinerary = query.ReturnDate.HasValue ? 2 : 1;

            // FlightType: true = Domestic (within Vietnam), false = International
            var flightType = await IsDomesticFlightAsync(query.Origin, query.Destination, ct);

            var entity = SearchAnalyticEntity.Create(
                agentCode: query.AgentCode,
                startPoint: query.Origin,
                endPoint: query.Destination,
                itinerary: itinerary,
                departDate: query.DepartDate,
                returnDate: query.ReturnDate,
                flightType: flightType,
                ipAddress: query.IpAddress,
                sources: sources);

            await searchAnalyticRepository.AddSearchAnalyticAsync(entity, ct);
        }
        catch (Exception ex)
        {
            // Never fail the search response because of analytics
            logger.LogWarning(ex, "Failed to track search analytics for {Origin}→{Destination}",
                query.Origin, query.Destination);
        }
    }

    /// <summary>
    /// Checks whether both airports belong to the same country ("VN").
    /// Falls back to false (international) if geo data is unavailable.
    /// </summary>
    private async Task<bool> IsDomesticFlightAsync(
        string origin, string destination, CancellationToken ct)
    {
        try
        {
            var countryCodes = await geoAirportRepository.GetCountryCodesAsync(
                new[] { origin, destination }, ct);

            return countryCodes.TryGetValue(origin, out var originCountry)
                && countryCodes.TryGetValue(destination, out var destCountry)
                && string.Equals(originCountry, destCountry, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false; // default to international if geo lookup fails
        }
    }

    // ── Engine allow / deny ───────────────────────────────────────────────────

    /// <summary>
    /// Returns true if the engine is permitted for this agent.
    /// Anonymous agents (no config) may use all engines.
    /// </summary>
    private static bool IsEngineAllowed(IFlightEngine engine, AgentConfigDto? config)
    {
        if (config is null) return true;

        return engine.Source switch
        {
            FlightSource.Galileo => config.GalileoActive,
            FlightSource.Datacom => config.DatacomActive,
            FlightSource.Kiwi    => config.KiwiActive,
            FlightSource.Pkfare  => config.PkfareActive,
            FlightSource.Maybay  => config.MaybayActive,
            _                    => true,
        };
    }

    /// <summary>
    /// Checks partner route eligibility based on IgnoredMode + ListStartPoint.
    /// Matches old Interface.cs CheckSearchFlight logic.
    ///
    /// IgnoredMode=0: "all routes EXCEPT these" (blacklist)
    /// IgnoredMode=1: "ONLY these routes" (whitelist)
    /// Route format: "{origin}{destination}" (e.g. "SGNHAN")
    ///
    /// Priority: whitelist first → blacklist → default allow.
    /// Conflicts: routes in both lists are removed from whitelist.
    /// </summary>
    private static bool IsRouteAllowedForEngine(
        IFlightEngine engine, AgentConfigDto? config, string origin, string destination)
    {
        if (config is null || config.PartnerRouteRules.Count == 0) return true;

        // Map engine source to partner name
        var partnerName = engine.Source switch
        {
            FlightSource.Kiwi    => "kiwi",
            FlightSource.Pkfare  => "pkfare",
            FlightSource.Maybay  => "maybay",
            FlightSource.Datacom => "datacom",
            _                    => null, // Galileo has no partner route rules
        };

        if (partnerName is null) return true;

        // Collect rules for this partner
        var rules = config.PartnerRouteRules
            .Where(r => r.PartnerName.Contains(partnerName))
            .ToList();

        if (rules.Count == 0) return true;

        var route = $"{origin}{destination}";
        var blacklist = new List<string>(); // IgnoredMode=0: all except these
        var whitelist = new List<string>(); // IgnoredMode=1: only these

        foreach (var rule in rules)
        {
            if (rule.IgnoredMode == 0)
                blacklist.AddRange(rule.Routes);
            else if (rule.IgnoredMode == 1)
                whitelist.AddRange(rule.Routes);
        }

        // Remove conflicts: routes in both lists → remove from whitelist
        whitelist.RemoveAll(x => blacklist.Contains(x));

        // Priority: whitelist first
        if (whitelist.Count > 0)
            return whitelist.Contains(route);

        // Then: blacklist
        if (blacklist.Count > 0)
            return !blacklist.Contains(route);

        // Default: allow
        return true;
    }

    // ── Airline ignore ────────────────────────────────────────────────────────

    /// <summary>
    /// Filters out fares that match any of the agent's airline ignore rules.
    ///
    /// Rule semantics (from agent_airline_ignores):
    ///   filter_by_plating     — suppresses if fare's PlatingCarrier matches
    ///   filter_by_any_segment — suppresses if ANY segment airline matches
    ///   filter_by_all_segments— suppresses if ALL segment airlines match
    /// </summary>
    private static List<FareDataDto> ApplyAirlineIgnores(
        List<FareDataDto>                    fares,
        IReadOnlyList<AirlineIgnoreDto> ignores)
    {
        return fares.Where(fare => !ShouldIgnore(fare, ignores)).ToList();
    }

    private static bool ShouldIgnore(FareDataDto fare, IReadOnlyList<AirlineIgnoreDto> ignores)
    {
        foreach (var rule in ignores)
        {
            var code = rule.AirlineCode;

            // Plating carrier = fare.Airline (the main carrier)
            if (rule.FilterByPlating &&
                string.Equals(fare.Airline, code, StringComparison.OrdinalIgnoreCase))
                return true;

            // Collect all segment airlines (outbound + return)
            var segmentAirlines = fare.OutboundSegments
                .Concat(fare.ReturnSegments)
                .Select(s => s.Airline)
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .ToList();

            if (rule.FilterByAnySegment &&
                segmentAirlines.Any(a => string.Equals(a, code, StringComparison.OrdinalIgnoreCase)))
                return true;

            if (rule.FilterByAllSegments && segmentAirlines.Count > 0 &&
                segmentAirlines.All(a => string.Equals(a, code, StringComparison.OrdinalIgnoreCase)))
                return true;
        }
        return false;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<IEnumerable<FareDataDto>> SafeSearchAsync(
        IFlightEngine engine, SearchFlightRequest request, CancellationToken ct)
    {
        try
        {
            return await engine.SearchFlightAsync(request, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Engine {Source} failed during search. Skipping.", engine.Source);
            return [];
        }
    }

    private static string BuildCacheKey(SearchFlightQuery q)
    {
        // AgentCode PHẢI nằm trong key vì commission adjust làm TotalFare khác nhau theo agent.
        // ReturnDate PHẢI nằm trong key để phân biệt one-way vs round-trip.
        string agent = string.IsNullOrWhiteSpace(q.AgentCode) ? "anon" : q.AgentCode.ToLowerInvariant();
        string returnPart = q.ReturnDate.HasValue ? q.ReturnDate.Value.ToString("yyyyMMdd") : "ow";
        return $"search:{agent}:{q.Origin}:{q.Destination}:{q.DepartDate:yyyyMMdd}:{returnPart}:{q.AdultCount}:{q.ChildCount}:{q.InfantCount}:{q.Currency}";
    }
}
