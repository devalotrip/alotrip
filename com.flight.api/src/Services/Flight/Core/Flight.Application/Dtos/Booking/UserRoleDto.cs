namespace Flight.Application.Dtos;

public sealed class UserRoleDto
{
    public int Id { get; init; }
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
}

public sealed class CreateUserRoleRequest
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
}