namespace Flight.Application.Dtos;

public sealed class AirlineDto
{
    public int Id { get; init; }
    public string Code { get; init; } = default!;
    public string? Name { get; init; }
    public string? Logo { get; init; }
    public bool Visible { get; init; }
}

public sealed class CreateAirlineRequest
{
    public string Code { get; set; } = default!;
    public string? Name { get; set; }
    public string? Logo { get; set; }
}