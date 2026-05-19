using Flight.Application.Dtos;
using Flight.Application.Helpers;
using Flight.Application.Interfaces;
using Flight.Domain.Enums;
using Flight.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Flight.Application.Features.ApplyCommission;

public sealed record ApplyCommissionCommand(
    IList<FareDataDto> Fares,
    string? AgentCode
) : IRequest<IList<FareDataDto>>;

public sealed class ApplyCommissionHandler(
    IAgentRepository repo,
    IGeoAirportRepository geoAirportRepo,
    ICurrencyRepository currencyRepository,
    ILogger<ApplyCommissionHandler> logger
) : IRequestHandler<ApplyCommissionCommand, IList<FareDataDto>>
{
    public async Task<IList<FareDataDto>> Handle(ApplyCommissionCommand request, CancellationToken ct)
    {
        var fares = request.Fares;
        var agentCode = request.AgentCode;

        if (string.IsNullOrWhiteSpace(agentCode) || fares.Count == 0)
            return fares;

        try
        {
            var commissions = await LoadCommissionsAsync(agentCode, ct);
            if (commissions.Count == 0)
                return fares;

            var airportCodes = fares
                .SelectMany(f => new[] { f.Origin, f.Destination })
                .Distinct()
                .ToList();
            var regionMap = await LoadAirportRegionsAsync(airportCodes, ct);

            var rateMap = (await currencyRepository.GetAllActiveAsync(ct))
                .ToDictionary(c => c.Code, c => (double)c.Rate, StringComparer.OrdinalIgnoreCase);

            bool isRoundTrip = fares.Any(f => f.TripType == TripType.RoundTrip);

            foreach (var fare in fares)
            {
                var commission = FindBestMatch(
                    commissions,
                    DetermineAirlineGroup(fare.Source),
                    regionMap.GetValueOrDefault(fare.Origin, ""),
                    regionMap.GetValueOrDefault(fare.Destination, ""));

                if (commission is null) continue;

                double exchangeRate = ResolveExchangeRate(commission.Currency, fare.Currency, rateMap);

                if (fare.AdultCount > 0)
                {
                    var (newBase, fee, total) = CommissionCalculator.Calculate(
                        commission, isRoundTrip, PassengerCategory.Adult,
                        (double)fare.AdultFare, (double)(fare.AdultFare + fare.TaxAmount), (double)fare.TaxAmount,
                        fare.Currency, exchangeRate);
                    fare.AdultFare = (decimal)newBase;
                    fare.ServiceFee = (decimal)fee;
                }

                if (fare.ChildCount > 0)
                {
                    var (newBase, fee, _) = CommissionCalculator.Calculate(
                        commission, isRoundTrip, PassengerCategory.Child,
                        (double)fare.ChildFare, (double)(fare.ChildFare + fare.TaxAmount), (double)fare.TaxAmount,
                        fare.Currency, exchangeRate);
                    fare.ChildFare = (decimal)newBase;
                    fare.ServiceFee += (decimal)fee;
                }

                if (fare.InfantCount > 0)
                {
                    var (newBase, fee, _) = CommissionCalculator.Calculate(
                        commission, isRoundTrip, PassengerCategory.Infant,
                        (double)fare.InfantFare, (double)(fare.InfantFare + fare.TaxAmount), (double)fare.TaxAmount,
                        fare.Currency, exchangeRate);
                    fare.InfantFare = (decimal)newBase;
                    fare.ServiceFee += (decimal)fee;
                }

                fare.TotalFare = (fare.AdultFare * fare.AdultCount)
                               + (fare.ChildFare * fare.ChildCount)
                               + (fare.InfantFare * fare.InfantCount)
                               + fare.TaxAmount
                               + fare.ServiceFee;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[ApplyCommission] Failed to apply commissions for agent {AgentCode}. Returning raw fares.", agentCode);
        }

        return fares;
    }

    private async Task<List<CommissionRecord>> LoadCommissionsAsync(string agentCode, CancellationToken ct)
    {
        var commissions = await repo.GetCommissionsAsync(null, ct);
        var agentCommissions = commissions.Where(c => c.AgentCode.Equals(agentCode, StringComparison.OrdinalIgnoreCase)).ToList();

        return agentCommissions.Select(c => new CommissionRecord
        {
            Id = c.Id,
            AgentId = c.AgentId,
            AirlineGroup = c.AirlineGroup,
            StartRegion = c.StartRegion,
            EndRegion = c.EndRegion,
            Currency = c.Currency,
            FeeAdtOneWay = (double)c.FeeAdtOneWay,
            FeeChdOneWay = (double)c.FeeChdOneWay,
            FeeInfOneWay = (double)c.FeeInfOneWay,
            FeeAdtRoundTrip = (double)c.FeeAdtRoundTrip,
            FeeChdRoundTrip = (double)c.FeeChdRoundTrip,
            FeeInfRoundTrip = (double)c.FeeInfRoundTrip,
            FeeByPercent = (double)c.FeeByPercent,
            Commission = (double)c.Commission,
            ComByPercent = c.ComByPercent,
            ComPercentOnBasicFare = c.ComPercentOnBaseFare
        }).ToList();
    }

    private async Task<Dictionary<string, string>> LoadAirportRegionsAsync(IEnumerable<string> airportCodes, CancellationToken ct)
    {
        try
        {
            return await geoAirportRepo.GetContinentCodesAsync(airportCodes, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[ApplyCommission] Failed to load airport regions — commission region matching will fall back to airline-group-only");
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static CommissionRecord? FindBestMatch(
        List<CommissionRecord> commissions,
        string airlineGroup,
        string startRegion,
        string endRegion)
    {
        var exact = commissions.FirstOrDefault(c =>
            c.AirlineGroup.Equals(airlineGroup, StringComparison.OrdinalIgnoreCase) &&
            c.StartRegion.Equals(startRegion, StringComparison.OrdinalIgnoreCase) &&
            c.EndRegion.Equals(endRegion, StringComparison.OrdinalIgnoreCase));
        if (exact is not null) return exact;

        var byGroup = commissions.FirstOrDefault(c =>
            c.AirlineGroup.Equals(airlineGroup, StringComparison.OrdinalIgnoreCase));
        return byGroup;
    }

    private static string DetermineAirlineGroup(FlightSource source) => source switch
    {
        FlightSource.Galileo => "DOM",
        FlightSource.Datacom => "LCC",
        FlightSource.Kiwi => "INT",
        FlightSource.Pkfare => "INT",
        FlightSource.Maybay => "LCC",
        _ => "DOM"
    };

    private static double ResolveExchangeRate(
        string commissionCurrency,
        string fareCurrency,
        Dictionary<string, double> rateMap)
    {
        if (commissionCurrency.Equals(fareCurrency, StringComparison.OrdinalIgnoreCase))
            return 1.0;

        if (rateMap.TryGetValue(commissionCurrency, out double fromRate) &&
            rateMap.TryGetValue(fareCurrency, out double toRate) &&
            fromRate > 0)
        {
            return toRate / fromRate;
        }

        return 1.0;
    }
}