namespace Flight.Infrastructure.Helpers;

/// <summary>
/// Flat record that mirrors the tblCommission SQL Server table.
/// Populated from PostgreSQL via EF Core (see CommissionConfiguration).
/// </summary>
public sealed record CommissionRecord
{
    public int     Id                  { get; init; }
    public int     AgentId             { get; init; }

    /// <summary>Airline type/group code (e.g. "DOM", "INT").</summary>
    public string  AirlineGroup        { get; init; } = string.Empty;

    /// <summary>IATA continent code of the origin country (e.g. "AS", "EU").</summary>
    public string  StartRegion         { get; init; } = string.Empty;

    /// <summary>IATA continent code of the destination country.</summary>
    public string  EndRegion           { get; init; } = string.Empty;

    /// <summary>3-letter ISO 4217 currency code for fee/commission amounts below.</summary>
    public string  Currency            { get; init; } = "VND";

    // ── Service fees ──────────────────────────────────────────────────────────

    public double FeeAdtOneWay      { get; init; }
    public double FeeChdOneWay      { get; init; }
    public double FeeInfOneWay      { get; init; }
    public double FeeAdtRoundTrip   { get; init; }
    public double FeeChdRoundTrip   { get; init; }
    public double FeeInfRoundTrip   { get; init; }

    /// <summary>Additional fee as a percentage of the fare (0–100).</summary>
    public double FeeByPercent      { get; init; }

    // ── Commission ────────────────────────────────────────────────────────────

    /// <summary>Commission amount (absolute) or percentage (0–100) — see <see cref="ComByPercent"/>.</summary>
    public double Commission        { get; init; }

    /// <summary>When <c>true</c>, <see cref="Commission"/> is a percentage; otherwise absolute amount.</summary>
    public bool   ComByPercent      { get; init; }

    /// <summary>
    /// When <c>true</c>, percentage commission is calculated on the base fare;
    /// otherwise on the total fare (base + tax).
    /// </summary>
    public bool   ComPercentOnBasicFare { get; init; }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Calculates service fees and commission for a single passenger type,
/// then applies VND / foreign-currency rounding.
///
/// Ported from the inline logic in GalileoEngine.cs:1789–1874.
/// </summary>
public static class CommissionCalculator
{
    /// <param name="commission">Commission record for the route+agent combination.</param>
    /// <param name="isRoundTrip">True for round-trip itinerary.</param>
    /// <param name="baseFare">Base fare for the passenger (before commission deduction).</param>
    /// <param name="totalFare">Total fare = base + tax (before fee addition).</param>
    /// <param name="tax">Tax component (passed through unchanged).</param>
    /// <param name="currencyCode">Output currency — drives rounding strategy.</param>
    /// <param name="exchangeRate">
    ///   Exchange rate multiplier: <c>commissionCurrency → outputCurrency</c>.
    ///   Pass 1.0 when both are the same currency.
    /// </param>
    /// <returns>
    ///   Tuple of (<c>newBaseFare</c>, <c>fee</c>, <c>newTotalFare</c>) after applying
    ///   commission deduction, fee addition, and rounding.
    /// </returns>
    public static (double BaseFare, double Fee, double TotalFare) Calculate(
        CommissionRecord commission,
        bool             isRoundTrip,
        PassengerCategory passengerCategory,
        double           baseFare,
        double           totalFare,
        double           tax,
        string           currencyCode,
        double           exchangeRate = 1.0)
    {
        double rawFee = passengerCategory switch
        {
            PassengerCategory.Adult    => isRoundTrip ? commission.FeeAdtRoundTrip : commission.FeeAdtOneWay,
            PassengerCategory.Child    => isRoundTrip ? commission.FeeChdRoundTrip : commission.FeeChdOneWay,
            PassengerCategory.Infant   => isRoundTrip ? commission.FeeInfRoundTrip : commission.FeeInfOneWay,
            _                          => 0
        };

        double fee = (rawFee * exchangeRate) + (commission.FeeByPercent * totalFare / 100.0);

        // Commission deduction from base fare
        double comAmt = commission.ComByPercent
            ? commission.ComPercentOnBasicFare
                ? commission.Commission * baseFare  / 100.0
                : commission.Commission * totalFare / 100.0
            : commission.Commission * exchangeRate;

        double newBaseFare  = baseFare  - comAmt;
        double newTotalFare = newBaseFare + tax + fee;

        // Rounding
        newBaseFare  = FlightEngineHelper.RoundFare(newBaseFare,  currencyCode);
        fee          = FlightEngineHelper.RoundFare(fee,          currencyCode);
        tax          = FlightEngineHelper.RoundFare(tax,          currencyCode);
        newTotalFare = FlightEngineHelper.RoundFare(newBaseFare + tax + fee, currencyCode);

        return (newBaseFare, fee, newTotalFare);
    }
}

// ─────────────────────────────────────────────────────────────────────────────

public enum PassengerCategory { Adult, Child, Infant }
