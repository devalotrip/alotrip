namespace Flight.Application.Dtos;

public sealed class TripVisaDto
{
    public int Id { get; init; }
    public Guid BookingId { get; init; }
    public string Code { get; init; } = default!;
    public string Name { get; init; } = default!;
    public decimal? Price { get; init; }
    public string? Value { get; init; }
    public string Currency { get; init; } = "VND";
    public decimal? PriceVn { get; init; }
}

public sealed class CreateTripVisaRequest
{
    public Guid BookingId { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public decimal? Price { get; set; }
    public string? Value { get; set; }
    public string Currency { get; set; } = "VND";
    public decimal? PriceVn { get; set; }
}