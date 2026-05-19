namespace Flight.Application.Dtos;

public sealed class PccDto
{
    public string Id { get; init; } = default!;
    public bool Active { get; init; }
}

public sealed class CreatePccRequest
{
    public string Pcc { get; set; } = default!;
}