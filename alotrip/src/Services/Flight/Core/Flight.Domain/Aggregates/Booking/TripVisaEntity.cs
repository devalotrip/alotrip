using Shared.BuildingBlocks.Abstractions;

namespace Flight.Domain.Aggregates.Booking;

public sealed class TripVisaEntity : Entity<int>
{
    public Guid BookingId { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public decimal? Price { get; set; }
    public string? Value { get; set; }
    public string Currency { get; set; } = "VND";
    public decimal? PriceVn { get; set; }

    private TripVisaEntity() { }

    public static TripVisaEntity Create(
        Guid bookingId, string code, string name, decimal? price,
        string? value, string currency, decimal? priceVn)
    {
        return new TripVisaEntity
        {
            BookingId = bookingId,
            Code = code,
            Name = name,
            Price = price,
            Value = value,
            Currency = currency ?? "VND",
            PriceVn = priceVn,
            CreatedOnUtc = DateTime.UtcNow
        };
    }
}