namespace Flight.Application.Dtos;

public sealed class AirlineIgnoreAdminDto
{
    public int Id { get; init; }
    public int AgentId { get; init; }
    public string Airline { get; init; } = default!;
    public bool FilterByPlatingCarrier { get; init; }
    public bool FilterByAnySegment { get; init; }
    public bool FilterByAllSegment { get; init; }
}

public sealed class CreateAirlineIgnoreRequest
{
    public int AgentId { get; set; }
    public string Airline { get; set; } = default!;
    public bool FilterByPlatingCarrier { get; set; }
    public bool FilterByAnySegment { get; set; }
    public bool FilterByAllSegment { get; set; }
}