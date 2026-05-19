using System.Data;
using Flight.Application.Dtos;
using Flight.Application.Interfaces;
using Flight.Domain.Enums;
using Flight.Infrastructure.Helpers;
using Flight.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Flight.Infrastructure.Services;

/// <summary>
/// Loads agent commissions from PostgreSQL and applies them to fare results.
///
/// Matching priority (most-specific wins):
///   1. agent_id + airline_group + start_region + end_region  (exact match)
///   2. agent_id + airline_group (any region)
///   3. no match → fares returned unchanged
/// </summary>
public sealed class CommissionService(
    ApplicationDbContext db,
    ICurrencyRepository currencyRepository,
    ILogger<CommissionService> logger)
    : ICommissionService
{
    // ── ICommissionService ────────────────────────────────────────────────────

    public async Task<IList<FareDataDto>> ApplyAsync(
        IList<FareDataDto> fares,
        string?            agentCode,
        CancellationToken  ct = default)
    {
        if (string.IsNullOrWhiteSpace(agentCode) || fares.Count == 0)
            return fares;

        try
        {
            // 1. Load all commissions for this agent via raw SQL
            var commissions = await LoadCommissionsAsync(agentCode, ct);
            if (commissions.Count == 0)
                return fares;

            // 2. Load geo lookup: airport → continent (best-effort)
            var airportCodes = fares
                .SelectMany(f => new[] { f.Origin, f.Destination })
                .Distinct()
                .ToList();
            var regionMap = await LoadAirportRegionsAsync(airportCodes, ct);

            // 3. Pre-load exchange rates once (rate = units per 1 USD base)
            var rateMap = (await currencyRepository.GetAllActiveAsync(ct))
                .ToDictionary(c => c.Code, c => (double)c.Rate, StringComparer.OrdinalIgnoreCase);

            // 4. Apply per fare
            bool isRoundTrip = fares.Any(f => f.TripType == TripType.RoundTrip);

            foreach (var fare in fares)
            {
                var commission = FindBestMatch(
                    commissions,
                    DetermineAirlineGroup(fare.Source),
                    regionMap.GetValueOrDefault(fare.Origin,  ""),
                    regionMap.GetValueOrDefault(fare.Destination, ""));

                if (commission is null) continue;

                double exchangeRate = ResolveExchangeRate(commission.Currency, fare.Currency, rateMap);

                // Adult
                if (fare.AdultCount > 0)
                {
                    var (newBase, fee, total) = CommissionCalculator.Calculate(
                        commission, isRoundTrip, PassengerCategory.Adult,
                        (double)fare.AdultFare, (double)(fare.AdultFare + fare.TaxAmount), (double)fare.TaxAmount,
                        fare.Currency, exchangeRate);
                    fare.AdultFare  = (decimal)newBase;
                    fare.ServiceFee = (decimal)fee;
                }

                // Child
                if (fare.ChildCount > 0)
                {
                    var (newBase, fee, _) = CommissionCalculator.Calculate(
                        commission, isRoundTrip, PassengerCategory.Child,
                        (double)fare.ChildFare, (double)(fare.ChildFare + fare.TaxAmount), (double)fare.TaxAmount,
                        fare.Currency, exchangeRate);
                    fare.ChildFare  = (decimal)newBase;
                    fare.ServiceFee += (decimal)fee;
                }

                // Infant
                if (fare.InfantCount > 0)
                {
                    var (newBase, fee, _) = CommissionCalculator.Calculate(
                        commission, isRoundTrip, PassengerCategory.Infant,
                        (double)fare.InfantFare, (double)(fare.InfantFare + fare.TaxAmount), (double)fare.TaxAmount,
                        fare.Currency, exchangeRate);
                    fare.InfantFare  = (decimal)newBase;
                    fare.ServiceFee += (decimal)fee;
                }

                // Recalculate total
                fare.TotalFare = (fare.AdultFare  * fare.AdultCount)
                               + (fare.ChildFare  * fare.ChildCount)
                               + (fare.InfantFare * fare.InfantCount)
                               + fare.TaxAmount
                               + fare.ServiceFee;
            }
        }
        catch (Exception ex)
        {
            // Commission errors must never kill a search result — log and continue
            logger.LogWarning(ex, "[Commission] Failed to apply commissions for agent {AgentCode}. Returning raw fares.", agentCode);
        }

        return fares;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<List<CommissionRecord>> LoadCommissionsAsync(
        string agentCode, CancellationToken ct)
    {
        const string sql = """
            SELECT
                c.id, c.agent_id,
                c.airline_group, c.start_region, c.end_region,
                c.currency,
                c.fee_adt_one_way,   c.fee_chd_one_way,   c.fee_inf_one_way,
                c.fee_adt_round_trip,c.fee_chd_round_trip,c.fee_inf_round_trip,
                c.fee_by_percent,
                c.commission, c.com_by_percent, c.com_percent_on_base_fare
            FROM commissions c
            INNER JOIN agents a ON a.id = c.agent_id
            WHERE a.agent_code = {0}
              AND a.active = TRUE
            """;

        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        // Use EF Core's raw SQL helper for non-entity projection
        var list = new List<CommissionRecord>();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql.Replace("{0}", "'"+agentCode.Replace("'", "''")+"'");
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new CommissionRecord
            {
                Id                   = reader.GetInt32(0),
                AgentId              = reader.GetInt32(1),
                AirlineGroup         = reader.GetString(2),
                StartRegion          = reader.GetString(3),
                EndRegion            = reader.GetString(4),
                Currency             = reader.GetString(5),
                FeeAdtOneWay         = reader.GetDouble(6),
                FeeChdOneWay         = reader.GetDouble(7),
                FeeInfOneWay         = reader.GetDouble(8),
                FeeAdtRoundTrip      = reader.GetDouble(9),
                FeeChdRoundTrip      = reader.GetDouble(10),
                FeeInfRoundTrip      = reader.GetDouble(11),
                FeeByPercent         = reader.GetDouble(12),
                Commission           = reader.GetDouble(13),
                ComByPercent         = reader.GetBoolean(14),
                ComPercentOnBasicFare= reader.GetBoolean(15)
            });
        }
        return list;
    }

    private async Task<Dictionary<string, string>> LoadAirportRegionsAsync(
        IEnumerable<string> airportCodes, CancellationToken ct)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!airportCodes.Any()) return result;

        try
        {
            const string sql = """
                SELECT ga.code, gc.continent_code
                FROM   geo_airports  ga
                JOIN   geo_cities    gci ON gci.code         = ga.city_code
                JOIN   geo_countries gc  ON gc.code          = gci.country_code
                WHERE  ga.code = ANY(@codes)
                """;

            var conn = db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            // Build param as literal IN list (safe since codes come from validated IATA)
            var quoted = string.Join(",", airportCodes.Select(c => $"'{c.Replace("'","''")}'"));
            cmd.CommandText = sql.Replace("ANY(@codes)", $"ANY(ARRAY[{quoted}])");
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                result[reader.GetString(0)] = reader.GetString(1);
        }
        catch
        {
            // Geo table may not exist yet — silently ignore
        }
        return result;
    }

    private static CommissionRecord? FindBestMatch(
        List<CommissionRecord> commissions,
        string airlineGroup,
        string startRegion,
        string endRegion)
    {
        // Priority 1: exact match
        var exact = commissions.FirstOrDefault(c =>
            c.AirlineGroup.Equals(airlineGroup, StringComparison.OrdinalIgnoreCase) &&
            c.StartRegion.Equals(startRegion,   StringComparison.OrdinalIgnoreCase) &&
            c.EndRegion.Equals(endRegion,       StringComparison.OrdinalIgnoreCase));
        if (exact is not null) return exact;

        // Priority 2: match airline group only
        var byGroup = commissions.FirstOrDefault(c =>
            c.AirlineGroup.Equals(airlineGroup, StringComparison.OrdinalIgnoreCase));
        return byGroup;
    }

    /// <summary>Maps FlightSource → airline_group used in tblCommission / commissions table.</summary>
    private static string DetermineAirlineGroup(FlightSource source) => source switch
    {
        FlightSource.Galileo => "DOM",  // refined further by airline type if needed
        FlightSource.Datacom => "LCC",
        FlightSource.Kiwi    => "INT",
        FlightSource.Pkfare  => "INT",
        FlightSource.Maybay  => "LCC",
        _                    => "DOM"
    };

    /// <summary>
    /// Converts a commission amount from <paramref name="commissionCurrency"/> to
    /// <paramref name="fareCurrency"/> using rates loaded from the currencies table.
    /// Both rates are assumed to be expressed as "units per 1 USD base" (same convention
    /// used by openexchangerates.org).
    ///
    /// Cross-rate formula: fareCurrency/commissionCurrency = rateF / rateC
    /// </summary>
    private static double ResolveExchangeRate(
        string commissionCurrency,
        string fareCurrency,
        Dictionary<string, double> rateMap)
    {
        if (commissionCurrency.Equals(fareCurrency, StringComparison.OrdinalIgnoreCase))
            return 1.0;

        if (rateMap.TryGetValue(commissionCurrency, out double fromRate) &&
            rateMap.TryGetValue(fareCurrency,        out double toRate)   &&
            fromRate > 0)
        {
            return toRate / fromRate;   // how many fareCurrency units per 1 commissionCurrency unit
        }

        // Rates not found in DB — log at trace and fall back to 1:1
        return 1.0;
    }
}
