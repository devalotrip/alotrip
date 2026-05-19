using Shared.BuildingBlocks.Abstractions;

namespace Flight.Domain.Aggregates.Booking;

public sealed class UserAccountEntity : Entity<int>
{
    public int? UserRoleId { get; set; }
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string? Phone { get; set; }
    public string? FullName { get; set; }
    public bool? Gender { get; set; }
    public string? Address { get; set; }
    public string? Avatar { get; set; }
    public DateTime? CreateDate { get; set; }
    public DateTime? LastLoginDate { get; set; }
    public string? IPLastLogin { get; set; }
    public bool Active { get; set; } = true;
    public bool Visible { get; set; } = true;
    public string? OTP { get; set; }

    private UserAccountEntity() { }

    public static UserAccountEntity Create(
        int? userRoleId, string email, string password,
        string? phone, string? fullName, bool? gender, string? address)
    {
        return new UserAccountEntity
        {
            UserRoleId = userRoleId,
            Email = email,
            Password = password,
            Phone = phone,
            FullName = fullName,
            Gender = gender,
            Address = address,
            CreateDate = DateTime.UtcNow,
            Active = true,
            Visible = true,
            CreatedOnUtc = DateTime.UtcNow
        };
    }
}