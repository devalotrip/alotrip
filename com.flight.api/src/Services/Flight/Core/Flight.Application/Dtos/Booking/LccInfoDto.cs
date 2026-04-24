namespace Flight.Application.Dtos;

public sealed class LccInfoDto
{
    public int Id { get; init; }
    public int AgentId { get; init; }
    public string Airline { get; init; } = default!;
    public bool AllowSearch { get; init; }
    public bool AllowBook { get; init; }
    public string? ProxyServerId { get; init; }
    public string? ProxyServerBookId { get; init; }
}

public sealed class CreateLccInfoRequest
{
    public int AgentId { get; set; }
    public string Airline { get; set; } = default!;
    public bool AllowSearch { get; set; }
    public bool AllowBook { get; set; }
}

public sealed class UpdateLccInfoRequest
{
    public bool? AllowSearch { get; set; }
    public bool? AllowBook { get; set; }
    public string? ProxyServerId { get; set; }
    public string? ProxyServerBookId { get; set; }
}