namespace Flight.Application.Dtos;

public sealed class PartnerDto
{
    public int Id { get; init; }
    public string Name { get; init; } = default!;
    public bool Active { get; init; }
}

public sealed class CreatePartnerRequest
{
    public string Name { get; set; } = default!;
}