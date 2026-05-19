namespace Flight.Infrastructure.Helpers;

/// <summary>
/// Detects whether a flight is operated by Pacific Airlines (BL / VN6x / VN4x codes).
/// Pacific Airlines was rebranded from Jetstar Pacific; its IATA code is BL but
/// Galileo/Travelport encodes it as VN6xx or VN4xx under Vietnam Airlines.
/// </summary>
public static class PacificAirlineHelper
{
    /// <summary>
    /// Returns <c>true</c> when the flight number indicates Pacific Airlines:
    /// <list type="bullet">
    ///   <item>Airline code is "BL"</item>
    ///   <item>Flight number starts with "VN6" or "VN4" (Galileo / Datacom / Kiwi / Pkfare / Maybay encoding)</item>
    /// </list>
    /// </summary>
    public static bool IsPacificAirline(string airlineCode, string flightNumber)
    {
        if (string.Equals(airlineCode, "BL", StringComparison.OrdinalIgnoreCase))
            return true;

        if (flightNumber.Length >= 3)
        {
            var prefix = flightNumber[..3];
            if (prefix == "VN6" || prefix == "VN4")
                return true;
        }

        return false;
    }

    /// <summary>
    /// Appends " - Pacific Airline" to the flight number when applicable
    /// (mirrors the legacy inline logic from every engine).
    /// </summary>
    public static string AppendPacificLabel(string airlineCode, string flightNumber)
        => IsPacificAirline(airlineCode, flightNumber)
            ? $"{flightNumber} - Pacific Airline"
            : flightNumber;

    /// <summary>
    /// Strips " - Pacific Airline" label from flight number.
    /// Used before sending booking requests or for clean comparisons.
    /// Matches old <c>flightNumber.Replace(" - Pacific Airline", "")</c> calls.
    /// </summary>
    public static string StripPacificLabel(string flightNumber)
        => flightNumber.Replace(" - Pacific Airline", "", StringComparison.Ordinal);

    /// <summary>
    /// Returns the GDS airline code for booking.
    /// Pacific Airlines (BL) is booked under Vietnam Airlines (VN) in all GDS systems.
    /// Matches old <c>departAirline = flt.AirlineCode == "BL" ? "VN" : flt.AirlineCode</c>.
    /// </summary>
    public static string GetBookingAirline(string airlineCode)
        => string.Equals(airlineCode, "BL", StringComparison.OrdinalIgnoreCase) ? "VN" : airlineCode;
}
