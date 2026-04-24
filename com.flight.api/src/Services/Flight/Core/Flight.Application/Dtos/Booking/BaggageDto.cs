namespace Flight.Application.Dtos;

public sealed class BaggageDto
{
    public int Id { get; init; }
    public string? BaggageCode { get; init; }
    public int? FlightId { get; init; }
    public string? FlightNumber { get; init; }
    public int? PaxId { get; init; }
    public Guid? BookingId { get; init; }
    public int? Weight { get; init; }
    public int? WeightUnit { get; init; }
    public int? PieceAllowance { get; init; }
    public string? CabinClass { get; init; }
    public string? BaggageType { get; init; }
}

public sealed class CreateBaggageRequest
{
    public string? BaggageCode { get; set; }
    public int? FlightId { get; set; }
    public string? FlightNumber { get; set; }
    public int? PaxId { get; set; }
    public Guid? BookingId { get; set; }
    public int? Weight { get; set; }
    public int? WeightUnit { get; set; }
    public int? PieceAllowance { get; set; }
    public string? CabinClass { get; set; }
    public string? BaggageType { get; set; }
}