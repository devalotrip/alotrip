namespace Flight.Application.Dtos;

public sealed class AgentInfoDto
{
    public int AgentId { get; init; }
    public string AgentCode { get; init; } = default!;
    public string User { get; init; } = default!;
    public string Password { get; init; } = default!;
    public string Currency { get; init; } = "VND";
    public bool Active { get; init; }
    public string? CompanyName { get; init; }
    public string? ContactEmail { get; init; }
    public string? ContactPhone { get; init; }
    public string? Address { get; init; }
}