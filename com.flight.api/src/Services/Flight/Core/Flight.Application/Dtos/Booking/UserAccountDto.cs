namespace Flight.Application.Dtos;

public sealed class UserAccountDto
{
    public int Id { get; init; }
    public int? UserRoleId { get; init; }
    public string Email { get; init; } = default!;
    public string? Phone { get; init; }
    public string? FullName { get; init; }
    public bool? Gender { get; init; }
    public string? Address { get; init; }
    public string? Avatar { get; init; }
    public DateTime? CreateDate { get; init; }
    public DateTime? LastLoginDate { get; init; }
    public string? IPLastLogin { get; init; }
    public bool Active { get; init; }
    public bool Visible { get; init; }
}

public sealed class CreateUserAccountRequest
{
    public int? UserRoleId { get; set; }
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string? Phone { get; set; }
    public string? FullName { get; set; }
    public bool? Gender { get; set; }
    public string? Address { get; set; }
    public bool Active { get; set; } = true;
}

public sealed class UpdateUserAccountRequest
{
    public int? UserRoleId { get; set; }
    public string? Phone { get; set; }
    public string? FullName { get; set; }
    public bool? Gender { get; set; }
    public string? Address { get; set; }
    public bool Active { get; set; }
    public string? OTP { get; set; }
}