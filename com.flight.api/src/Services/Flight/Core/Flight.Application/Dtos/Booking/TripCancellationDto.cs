namespace Flight.Application.Dtos;

public sealed class TripCancellationDto
{
    public int Id { get; init; }
    public Guid BookingId { get; init; }
    public decimal MarkupAmount { get; init; }
    public decimal MarkupPercent { get; init; }
    public decimal Price { get; init; }
    public string Currency { get; init; } = "VND";
    public decimal BookingPrice { get; init; }
    public string? Value { get; init; }
}

public sealed class CreateTripCancellationRequest
{
    public Guid BookingId { get; set; }
    public decimal MarkupAmount { get; set; }
    public decimal MarkupPercent { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "VND";
    public decimal BookingPrice { get; set; }
    public string? Value { get; set; }
}