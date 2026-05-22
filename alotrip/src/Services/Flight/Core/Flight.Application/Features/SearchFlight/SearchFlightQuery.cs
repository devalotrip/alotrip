using Flight.Application.Dtos;
using Flight.Application.Features.ApplyCommission;
using Flight.Application.Interfaces;
using Flight.Domain.Aggregates.Booking;
using Flight.Domain.Enums;
using Flight.Domain.Repositories;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
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
    IFlightCacheService        _,       // Reserved for future cache re-enable
    ISender                    sender,
    IAgentConfigRepository     agentConfigRepository,
    IAgentRepository           agentRepository,
    ISearchAnalyticRepository  searchAnalyticRepository,
    IGeoAirportRepository      geoAirportRepository,
    IConfiguration             configuration,
    ILogger<SearchFlightQueryHandler> logger)
    : IQueryHandler<SearchFlightQuery, IEnumerable<FareDataDto>>
{
    public async Task<IEnumerable<FareDataDto>> Handle(
        SearchFlightQuery query, CancellationToken ct)
    {
        // 1. Build cache key
        var cacheKey = BuildCacheKey(query);

        // 2. Load agent config
        AgentConfigDto? agentConfig = null;
        if (!string.IsNullOrWhiteSpace(query.AgentCode))
            agentConfig = await agentConfigRepository.GetByCodeAsync(query.AgentCode, ct);

        // 3. Get country codes for route check
        var countryCodes = await geoAirportRepository.GetCountryCodesAsync(
            new[] { query.Origin, query.Destination }, ct);
        var originCountry = countryCodes.GetValueOrDefault(query.Origin, "").ToUpperInvariant();
        var destCountry = countryCodes.GetValueOrDefault(query.Destination, "").ToUpperInvariant();
        bool isInternational = originCountry != "VN" || destCountry != "VN";

        // 4. Get minimum depart time from config (default 0 hours)
        int minimumDepartHours = int.Parse(configuration["MinimumDepartTime"] ?? "0");

        // 5. Fan-out song song đến các engines
        var allFares = new List<FareDataDto>();

        // ── Galileo: chỉ search quốc tế + loop theo từng PCC của agent ────────
        if (isInternational && agentConfig?.GalileoActive == true)
        {
            var galileoEngine = engines.FirstOrDefault(e => e.Source == FlightSource.Galileo);
            if (galileoEngine is not null)
            {
                var galileoFares = await SearchGalileoWithPccLoopAsync(
                    galileoEngine, query, agentConfig, minimumDepartHours, ct);
                allFares.AddRange(galileoFares);
            }
        }

        // ── Partner engines: Kiwi, Pkfare, Maybay, Datacom ────────────────────
        var partnerEngines = engines.Where(e => e.Source != FlightSource.Galileo).ToList();
        var partnerTasks = new List<Task<IEnumerable<FareDataDto>>>();

        foreach (var engine in partnerEngines)
        {
            if (!engine.IsEnabled) continue;
            if (!IsEngineAllowed(engine, agentConfig)) continue;
            if (!IsRouteAllowedForEngine(engine, agentConfig, query.Origin, query.Destination, originCountry, destCountry)) continue;

            partnerTasks.Add(SafeSearchAsync(engine, query, minimumDepartHours, ct));
        }

        var partnerResults = await Task.WhenAll(partnerTasks);
        foreach (var result in partnerResults)
            allFares.AddRange(result);

        logger.LogInformation("Found {FareCount} raw fares for {Origin}→{Destination}",
            allFares.Count, query.Origin, query.Destination);

        // 6. Apply airline ignore filters
        if (agentConfig?.AirlineIgnores.Count > 0)
            allFares = ApplyAirlineIgnores(allFares, agentConfig.AirlineIgnores);

        // 7. Apply agent commission / service fee
        if (allFares.Count > 0 && !string.IsNullOrWhiteSpace(query.AgentCode))
        {
            var applied = await sender.Send(new ApplyCommissionCommand(allFares, query.AgentCode), ct);
            allFares = applied.ToList();
        }

        // Re-sort after commission adjustment
        allFares.Sort((a, b) => a.TotalFare.CompareTo(b.TotalFare));

        // 8. Track search analytics
        var sourcesStr = string.Join(",", allFares.Select(f => f.Source.ToString()).Distinct());
        await TrackSearchAnalyticAsync(query, agentConfig, sourcesStr, ct);

        return allFares;
    }

    // ── Galileo PCC loop (matches old Interface.cs logic) ─────────────────────

    /// <summary>
    /// Search Galileo with PCC loop — matches old Interface.cs Travelport section.
    /// Each allowed PCC runs as a separate task, results are merged.
    /// </summary>
    private async Task<List<FareDataDto>> SearchGalileoWithPccLoopAsync(
        IFlightEngine galileoEngine,
        SearchFlightQuery query,
        AgentConfigDto agentConfig,
        int minimumDepartHours,
        CancellationToken ct)
    {
        if (agentConfig.AgentId == 0) return [];

        // Get active PCCs for this agent
        var activePccs = await agentRepository.GetActivePccsByAgentIdAsync(agentConfig.AgentId, ct);
        var allowedPccs = new List<AgentPccEntity>();

        foreach (var pcc in activePccs)
        {
            var arrRoute = (pcc.ListStartPoint ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (pcc.IgnoredMode == 0)
            {
                // Blacklist: search all EXCEPT these routes
                if (arrRoute.Length == 0 || !IsRouteInList(arrRoute, query.Origin, query.Destination, query.Origin, query.Destination))
                    allowedPccs.Add(pcc);
            }
            else if (pcc.IgnoredMode == 1)
            {
                // Whitelist: search ONLY these routes
                if (IsRouteInList(arrRoute, query.Origin, query.Destination, query.Origin, query.Destination))
                    allowedPccs.Add(pcc);
            }
            else
            {
                // No restriction
                allowedPccs.Add(pcc);
            }
        }

        if (allowedPccs.Count == 0) return [];

        // Fan-out: each PCC runs as separate task (matches old Task.Factory.StartNew)
        var tasks = allowedPccs.Select(pcc => SafeSearchGalileoAsync(galileoEngine, query, pcc.Pcc, minimumDepartHours, ct));
        var results = await Task.WhenAll(tasks);

        return results.SelectMany(r => r).ToList();
    }

    /// <summary>
    /// Checks if the route matches any pattern in the list.
    /// Matches old Interface.cs CheckSearchFlight 5-pattern logic:
    ///   startPoint, startPoint+endPoint, startCountry+endCountry, startCountry+endPoint, startPoint+endCountry
    /// </summary>
    private static bool IsRouteInList(string[] routeList, string origin, string destination, string originCountry, string destCountry)
    {
        return routeList.Any(r =>
            r.Trim().Equals(origin, StringComparison.OrdinalIgnoreCase) ||
            r.Trim().Equals(origin + destination, StringComparison.OrdinalIgnoreCase) ||
            r.Trim().Equals(originCountry + destCountry, StringComparison.OrdinalIgnoreCase) ||
            r.Trim().Equals(originCountry + destination, StringComparison.OrdinalIgnoreCase) ||
            r.Trim().Equals(origin + destCountry, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<IEnumerable<FareDataDto>> SafeSearchGalileoAsync(
        IFlightEngine engine, SearchFlightQuery query, string pccCode, int minimumDepartHours, CancellationToken ct)
    {
        try
        {
            var request = new SearchFlightRequest
            {
                Origin      = query.Origin,
                Destination = query.Destination,
                DepartDate  = query.DepartDate,
                ReturnDate  = query.ReturnDate,
                TripType    = query.ReturnDate.HasValue ? TripType.RoundTrip : TripType.OneWay,
                AdultCount  = query.AdultCount,
                ChildCount  = query.ChildCount,
                InfantCount = query.InfantCount,
                Currency    = query.Currency,
                AgentCode   = query.AgentCode,
                PccCode     = pccCode
            };

            var fares = await engine.SearchFlightAsync(request, ct);

            // Apply minimum depart time filter (matches old code)
            return fares.Where(f =>
                f.OutboundOptions.All(o => o.Segments.All(s => (s.DepartTime - DateTime.Now).TotalHours >= minimumDepartHours)) &&
                f.ReturnOptions.All(o => o.Segments.All(s => (s.DepartTime - DateTime.Now).TotalHours >= minimumDepartHours))
            );
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Galileo PCC {Pcc} failed during search. Skipping.", pccCode);
            return [];
        }
    }

    // ── Partner engine search ─────────────────────────────────────────────────

    private async Task<IEnumerable<FareDataDto>> SafeSearchAsync(
        IFlightEngine engine, SearchFlightQuery query, int minimumDepartHours, CancellationToken ct)
    {
        try
        {
            var request = new SearchFlightRequest
            {
                Origin      = query.Origin,
                Destination = query.Destination,
                DepartDate  = query.DepartDate,
                ReturnDate  = query.ReturnDate,
                TripType    = query.ReturnDate.HasValue ? TripType.RoundTrip : TripType.OneWay,
                AdultCount  = query.AdultCount,
                ChildCount  = query.ChildCount,
                InfantCount = query.InfantCount,
                Currency    = query.Currency,
                AgentCode   = query.AgentCode
            };

            var fares = await engine.SearchFlightAsync(request, ct);

            // Apply minimum depart time filter
            return fares.Where(f =>
                f.OutboundOptions.All(o => o.Segments.All(s => (s.DepartTime - DateTime.Now).TotalHours >= minimumDepartHours)) &&
                f.ReturnOptions.All(o => o.Segments.All(s => (s.DepartTime - DateTime.Now).TotalHours >= minimumDepartHours))
            );
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Engine {Source} failed during search. Skipping.", engine.Source);
            return [];
        }
    }

    // ── Engine allow / deny ───────────────────────────────────────────────────

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
    /// Matches old Interface.cs CheckSearchFlight logic with 5-pattern matching.
    ///
    /// IgnoredMode=0: "all routes EXCEPT these" (blacklist)
    /// IgnoredMode=1: "ONLY these routes" (whitelist)
    ///
    /// 5 patterns checked (from old code):
    ///   1. startPoint                           (e.g. "SGN")
    ///   2. startPoint + endPoint                (e.g. "SGNHAN")
    ///   3. startCountry + endCountry            (e.g. "VNJP")
    ///   4. startCountry + endPoint              (e.g. "VNHAN")
    ///   5. startPoint + endCountry              (e.g. "SGNJP")
    ///
    /// Priority: whitelist first → blacklist → default allow.
    /// Conflicts: routes in both lists are removed from whitelist.
    /// </summary>
    private static bool IsRouteAllowedForEngine(
        IFlightEngine engine, AgentConfigDto? config,
        string origin, string destination, string originCountry, string destCountry)
    {
        if (config is null || config.PartnerRouteRules.Count == 0) return true;

        // Map engine source to partner name
        var partnerName = engine.Source switch
        {
            FlightSource.Kiwi    => "kiwi",
            FlightSource.Pkfare  => "pkfare",
            FlightSource.Maybay  => "maybay",
            FlightSource.Datacom => "datacom",
            _                    => null,
        };

        if (partnerName is null) return true;

        // Collect rules for this partner
        var rules = config.PartnerRouteRules
            .Where(r => r.PartnerName.Contains(partnerName))
            .ToList();

        if (rules.Count == 0) return true;

        var blacklist = new List<string>();
        var whitelist = new List<string>();

        foreach (var rule in rules)
        {
            if (rule.IgnoredMode == 0)
                blacklist.AddRange(rule.Routes);
            else if (rule.IgnoredMode == 1)
                whitelist.AddRange(rule.Routes);
        }

        // Remove conflicts
        whitelist.RemoveAll(x => blacklist.Contains(x));

        // Check if route matches any pattern in the lists (5-pattern matching)
        bool IsMatch(List<string> list) => list.Any(r =>
            r.Trim().Equals(origin, StringComparison.OrdinalIgnoreCase) ||
            r.Trim().Equals(origin + destination, StringComparison.OrdinalIgnoreCase) ||
            r.Trim().Equals(originCountry + destCountry, StringComparison.OrdinalIgnoreCase) ||
            r.Trim().Equals(originCountry + destination, StringComparison.OrdinalIgnoreCase) ||
            r.Trim().Equals(origin + destCountry, StringComparison.OrdinalIgnoreCase));

        // Priority: whitelist first
        if (whitelist.Count > 0)
            return IsMatch(whitelist);

        // Then: blacklist
        if (blacklist.Count > 0)
            return !IsMatch(blacklist);

        // Default: allow
        return true;
    }

    // ── Airline ignore ────────────────────────────────────────────────────────

    private static List<FareDataDto> ApplyAirlineIgnores(
        List<FareDataDto> fares, IReadOnlyList<AirlineIgnoreDto> ignores)
    {
        return fares.Where(fare => !ShouldIgnore(fare, ignores)).ToList();
    }

    private static bool ShouldIgnore(FareDataDto fare, IReadOnlyList<AirlineIgnoreDto> ignores)
    {
        foreach (var rule in ignores)
        {
            var code = rule.AirlineCode;

            if (rule.FilterByPlating &&
                string.Equals(fare.Airline, code, StringComparison.OrdinalIgnoreCase))
                return true;

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

    // ── Search analytics tracking ─────────────────────────────────────────────

    private async Task TrackSearchAnalyticAsync(
        SearchFlightQuery query, AgentConfigDto? agentConfig, string sources, CancellationToken ct)
    {
        try
        {
            var itinerary = query.ReturnDate.HasValue ? 2 : 1;
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
            logger.LogWarning(ex, "Failed to track search analytics for {Origin}→{Destination}",
                query.Origin, query.Destination);
        }
    }

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
            return false;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string BuildCacheKey(SearchFlightQuery q)
    {
        string agent = string.IsNullOrWhiteSpace(q.AgentCode) ? "anon" : q.AgentCode.ToLowerInvariant();
        string returnPart = q.ReturnDate.HasValue ? q.ReturnDate.Value.ToString("yyyyMMdd") : "ow";
        return $"search:{agent}:{q.Origin}:{q.Destination}:{q.DepartDate:yyyyMMdd}:{returnPart}:{q.AdultCount}:{q.ChildCount}:{q.InfantCount}:{q.Currency}";
    }
}
