namespace Flight.Application.Dtos;

public sealed class CarRentalDto
{
    public int Id { get; init; }
    public Guid BookingId { get; init; }
    public string Provider { get; init; } = default!;
    public string PickupLocation { get; init; } = default!;
    public string DropoffLocation { get; init; } = default!;
    public DateTime PickupDate { get; init; }
    public DateTime DropoffDate { get; init; }
    public string CarType { get; init; } = default!;
    public string? CarModel { get; init; }
    public decimal DailyRate { get; init; }
    public int TotalDays { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = "VND";
    public string Status { get; init; } = "Confirmed";
}

public sealed class CreateCarRentalRequest
{
    public Guid BookingId { get; set; }
    public string Provider { get; set; } = default!;
    public string PickupLocation { get; set; } = default!;
    public string DropoffLocation { get; set; } = default!;
    public DateTime PickupDate { get; set; }
    public DateTime DropoffDate { get; set; }
    public string CarType { get; set; } = default!;
    public string? CarModel { get; set; }
    public decimal DailyRate { get; set; }
    public string Currency { get; set; } = "VND";
}