namespace Flight.Application.Helpers;

public sealed record CommissionRecord
{
    public int Id { get; init; }
    public int AgentId { get; init; }
    public string AirlineGroup { get; init; } = string.Empty;
    public string StartRegion { get; init; } = string.Empty;
    public string EndRegion { get; init; } = string.Empty;
    public string Currency { get; init; } = "VND";
    public double FeeAdtOneWay { get; init; }
    public double FeeChdOneWay { get; init; }
    public double FeeInfOneWay { get; init; }
    public double FeeAdtRoundTrip { get; init; }
    public double FeeChdRoundTrip { get; init; }
    public double FeeInfRoundTrip { get; init; }
    public double FeeByPercent { get; init; }
    public double Commission { get; init; }
    public bool ComByPercent { get; init; }
    public bool ComPercentOnBasicFare { get; init; }
}

public enum PassengerCategory { Adult, Child, Infant }

public static class CommissionCalculator
{
    public static (double BaseFare, double Fee, double TotalFare) Calculate(
        CommissionRecord commission,
        bool isRoundTrip,
        PassengerCategory passengerCategory,
        double baseFare,
        double totalFare,
        double tax,
        string currencyCode,
        double exchangeRate = 1.0)
    {
        double rawFee = passengerCategory switch
        {
            PassengerCategory.Adult => isRoundTrip ? commission.FeeAdtRoundTrip : commission.FeeAdtOneWay,
            PassengerCategory.Child => isRoundTrip ? commission.FeeChdRoundTrip : commission.FeeChdOneWay,
            PassengerCategory.Infant => isRoundTrip ? commission.FeeInfRoundTrip : commission.FeeInfOneWay,
            _ => 0
        };

        double fee = (rawFee * exchangeRate) + (commission.FeeByPercent * totalFare / 100.0);

        double comAmt = commission.ComByPercent
            ? commission.ComPercentOnBasicFare
                ? commission.Commission * baseFare / 100.0
                : commission.Commission * totalFare / 100.0
            : commission.Commission * exchangeRate;

        double newBaseFare = baseFare - comAmt;
        double newTotalFare = newBaseFare + tax + fee;

        newBaseFare = Math.Round(newBaseFare, currencyCode == "VND" ? -3 : 2);
        fee = Math.Round(fee, currencyCode == "VND" ? -3 : 2);
        tax = Math.Round(tax, currencyCode == "VND" ? -3 : 2);
        newTotalFare = Math.Round(newBaseFare + tax + fee, currencyCode == "VND" ? -3 : 2);

        return (newBaseFare, fee, newTotalFare);
    }
}