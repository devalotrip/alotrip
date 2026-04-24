using Shared.BuildingBlocks.Abstractions;

namespace Flight.Domain.Aggregates.Booking;

public sealed class TripTourEntity : Entity<int>
{
    public Guid BookingId { get; set; }
    public string TourName { get; set; } = default!;
    public string TourCode { get; set; } = default!;
    public DateTime DepartureDate { get; set; }
    public DateTime ReturnDate { get; set; }
    public string Destination { get; set; } = default!;
    public int PaxCount { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "VND";
    public string Status { get; set; } = "Confirmed";

    private TripTourEntity() { }

    public static TripTourEntity Create(
        Guid bookingId, string tourName, string tourCode,
        DateTime departureDate, DateTime returnDate, string destination,
        int paxCount, decimal unitPrice, decimal totalAmount, string currency = "VND")
    {
        return new TripTourEntity
        {
            BookingId = bookingId,
            TourName = tourName,
            TourCode = tourCode,
            DepartureDate = departureDate,
            ReturnDate = returnDate,
            Destination = destination,
            PaxCount = paxCount,
            UnitPrice = unitPrice,
            TotalAmount = totalAmount,
            Currency = currency,
            CreatedOnUtc = DateTime.UtcNow
        };
    }
}