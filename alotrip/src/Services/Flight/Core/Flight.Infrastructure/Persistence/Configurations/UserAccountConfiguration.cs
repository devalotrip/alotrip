using Flight.Domain.Aggregates.Booking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flight.Infrastructure.Persistence.Configurations;

public sealed class UserAccountConfiguration : IEntityTypeConfiguration<UserAccountEntity>
{
    public void Configure(EntityTypeBuilder<UserAccountEntity> builder)
    {
        builder.ToTable("user_accounts");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(e => e.UserRoleId).HasColumnName("user_role_id");
        builder.Property(e => e.Email).HasColumnName("email").HasMaxLength(150).IsRequired();
        builder.Property(e => e.Password).HasColumnName("password").HasMaxLength(200).IsRequired();
        builder.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(20);
        builder.Property(e => e.FullName).HasColumnName("full_name").HasMaxLength(200);
        builder.Property(e => e.Gender).HasColumnName("gender");
        builder.Property(e => e.Address).HasColumnName("address").HasMaxLength(500);
        builder.Property(e => e.Avatar).HasColumnName("avatar").HasMaxLength(500);
        builder.Property(e => e.CreateDate).HasColumnName("create_date");
        builder.Property(e => e.LastLoginDate).HasColumnName("last_login_date");
        builder.Property(e => e.IPLastLogin).HasColumnName("ip_last_login").HasMaxLength(50);
        builder.Property(e => e.Active).HasColumnName("active");
        builder.Property(e => e.Visible).HasColumnName("visible");
        builder.Property(e => e.OTP).HasColumnName("otp").HasMaxLength(10);

        builder.Property(e => e.CreatedOnUtc).HasColumnName("created_on_utc");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
    }
}