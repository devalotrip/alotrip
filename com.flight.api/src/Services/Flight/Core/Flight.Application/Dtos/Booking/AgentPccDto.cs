namespace Flight.Application.Dtos;

public sealed class AgentPccDto
{
    public int Id { get; init; }
    public int AgentId { get; init; }
    public string Pcc { get; init; } = default!;
    public int IgnoredMode { get; init; }
    public string? ListStartPoint { get; init; }
    public bool Active { get; init; }
}

public sealed class CreateAgentPccRequest
{
    public int AgentId { get; set; }
    public string Pcc { get; set; } = default!;
    public int IgnoredMode { get; set; }
    public string? ListStartPoint { get; set; }
}