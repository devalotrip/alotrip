using Shared.BuildingBlocks.Abstractions;

namespace Flight.Domain.Aggregates.Booking;

/// <summary>
/// Một chặng bay trong booking (có thể có nhiều segment). Map từ tblBookingFlight cũ.
/// </summary>
public sealed class BookingFlightEntity : Entity<Guid>
{
    public Guid   BookingId   { get; private set; }
    public string Origin      { get; private set; } = default!;
    public string Destination { get; private set; } = default!;
    public DateTime DepartTime { get; private set; }
    public DateTime ArriveTime { get; private set; }
    public int    StopCount   { get; private set; }
    public string Airline     { get; private set; } = default!;

    private readonly List<BookingSegmentEntity> _segments = [];
    public IReadOnlyList<BookingSegmentEntity> Segments => _segments.AsReadOnly();

    private BookingFlightEntity() { }

    public static BookingFlightEntity Create(
        Guid bookingId, string origin, string destination,
        DateTime departTime, DateTime arriveTime,
        string airline, int stopCount = 0)
    {
        return new BookingFlightEntity
        {
            Id          = Guid.NewGuid(),
            BookingId   = bookingId,
            Origin      = origin,
            Destination = destination,
            DepartTime  = departTime,
            ArriveTime  = arriveTime,
            Airline     = airline,
            StopCount   = stopCount,
            CreatedOnUtc = DateTime.UtcNow
        };
    }

    public void AddSegment(BookingSegmentEntity segment)
        => _segments.Add(segment);
}
