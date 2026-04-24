namespace Flight.Application.Dtos;

public sealed class AircraftDto
{
    public int Id { get; init; }
    public string IATA { get; init; } = default!;
    public string? Manufacturer { get; init; }
    public string? Model { get; init; }
    public bool Visible { get; init; }
}

public sealed class CreateAircraftRequest
{
    public string IATA { get; set; } = default!;
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
}