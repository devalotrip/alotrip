namespace Flight.Application.Dtos;

public sealed class AirlineTypeDto
{
    public int Id { get; init; }
    public string Code { get; init; } = default!;
    public string? Name { get; init; }
    public string? Description { get; init; }
    public bool Visible { get; init; }
}

public sealed class CreateAirlineTypeRequest
{
    public string Code { get; set; } = default!;
    public string? Name { get; set; }
    public string? Description { get; set; }
}