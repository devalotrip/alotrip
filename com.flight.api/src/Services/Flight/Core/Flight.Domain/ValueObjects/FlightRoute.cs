using Flight.Domain.Exceptions;

namespace Flight.Domain.ValueObjects;

/// <summary>
/// Hành trình bay: điểm đi → điểm đến + ngày bay.
/// Map từ SearchParam cũ (From/To/DateFrom).
/// </summary>
public sealed record FlightRoute
{
    public AirportCode Origin      { get; }
    public AirportCode Destination { get; }
    public DateOnly    DepartDate  { get; }

    private FlightRoute(AirportCode origin, AirportCode destination, DateOnly departDate)
    {
        Origin      = origin;
        Destination = destination;
        DepartDate  = departDate;
    }

    public static FlightRoute Of(string origin, string destination, DateOnly departDate)
    {
        var o = AirportCode.Of(origin);
        var d = AirportCode.Of(destination);

        if (o == d)
            throw new DomainException("Origin and destination cannot be the same airport.");
        if (departDate < DateOnly.FromDateTime(DateTime.UtcNow.Date))
            throw new DomainException("Departure date cannot be in the past.");

        return new FlightRoute(o, d, departDate);
    }

    public override string ToString() => $"{Origin} → {Destination} ({DepartDate:yyyy-MM-dd})";
}
