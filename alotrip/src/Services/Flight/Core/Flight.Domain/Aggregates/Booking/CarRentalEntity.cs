using Shared.BuildingBlocks.Abstractions;

namespace Flight.Domain.Aggregates.Booking;

public sealed class CarRentalEntity : Entity<int>
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
    public int TotalDays { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "VND";
    public string Status { get; set; } = "Confirmed";

    private CarRentalEntity() { }

    public static CarRentalEntity Create(
        Guid bookingId, string provider, string pickupLocation, string dropoffLocation,
        DateTime pickupDate, DateTime dropoffDate, string carType, string? carModel,
        decimal dailyRate, int totalDays, decimal totalAmount, string currency = "VND")
    {
        return new CarRentalEntity
        {
            BookingId = bookingId,
            Provider = provider,
            PickupLocation = pickupLocation,
            DropoffLocation = dropoffLocation,
            PickupDate = pickupDate,
            DropoffDate = dropoffDate,
            CarType = carType,
            CarModel = carModel,
            DailyRate = dailyRate,
            TotalDays = totalDays,
            TotalAmount = totalAmount,
            Currency = currency,
            CreatedOnUtc = DateTime.UtcNow
        };
    }
}