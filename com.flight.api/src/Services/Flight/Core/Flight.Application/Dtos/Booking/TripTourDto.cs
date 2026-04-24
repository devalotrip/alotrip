namespace Flight.Application.Dtos;

public sealed class TripTourDto
{
    public int Id { get; init; }
    public Guid BookingId { get; init; }
    public string TourName { get; init; } = default!;
    public string TourCode { get; init; } = default!;
    public DateTime DepartureDate { get; init; }
    public DateTime ReturnDate { get; init; }
    public string Destination { get; init; } = default!;
    public int PaxCount { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = "VND";
    public string Status { get; init; } = "Confirmed";
}

public sealed class CreateTripTourRequest
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
}