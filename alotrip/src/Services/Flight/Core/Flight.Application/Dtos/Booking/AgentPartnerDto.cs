namespace Flight.Application.Dtos;

public sealed class AgentPartnerDto
{
    public int Id { get; init; }
    public int AgentId { get; init; }
    public int PartnerId { get; init; }
    public int IgnoredMode { get; init; }
    public string? ListStartPoint { get; init; }
    public bool Active { get; init; }
}

public sealed class CreateAgentPartnerRequest
{
    public int AgentId { get; set; }
    public int PartnerId { get; set; }
    public int IgnoredMode { get; set; }
    public string? ListStartPoint { get; set; }
}