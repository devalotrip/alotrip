using Shared.BuildingBlocks.Abstractions;

namespace Flight.Domain.Aggregates.Booking;

/// <summary>
/// Segment của một chặng bay (leg). Map từ tblBookingSegment cũ.
/// </summary>
public sealed class BookingSegmentEntity : Entity<Guid>
{
    public Guid   BookingFlightId { get; private set; }
    public string FlightNumber    { get; private set; } = default!;
    public string Airline         { get; private set; } = default!;
    public string Origin          { get; private set; } = default!;
    public string Destination     { get; private set; } = default!;
    public DateTime DepartTime    { get; private set; }
    public DateTime ArriveTime    { get; private set; }
    public string CabinClass      { get; private set; } = default!;
    public string? AircraftType   { get; private set; }

    private BookingSegmentEntity() { }

    public static BookingSegmentEntity Create(
        Guid bookingFlightId, string flightNumber, string airline,
        string origin, string destination,
        DateTime departTime, DateTime arriveTime,
        string cabinClass, string? aircraftType = null)
    {
        return new BookingSegmentEntity
        {
            Id              = Guid.NewGuid(),
            BookingFlightId = bookingFlightId,
            FlightNumber    = flightNumber,
            Airline         = airline,
            Origin          = origin,
            Destination     = destination,
            DepartTime      = departTime,
            ArriveTime      = arriveTime,
            CabinClass      = cabinClass,
            AircraftType    = aircraftType,
            CreatedOnUtc    = DateTime.UtcNow
        };
    }
}
