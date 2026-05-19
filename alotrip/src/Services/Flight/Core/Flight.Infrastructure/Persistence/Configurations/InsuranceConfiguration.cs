using Flight.Domain.Aggregates.Booking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flight.Infrastructure.Persistence.Configurations;

public sealed class InsuranceConfiguration : IEntityTypeConfiguration<InsuranceEntity>
{
    public void Configure(EntityTypeBuilder<InsuranceEntity> builder)
    {
        builder.ToTable("insurances");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(e => e.BookingId).HasColumnName("booking_id");
        builder.Property(e => e.PassengerId).HasColumnName("passenger_id");
        builder.Property(e => e.Provider).HasColumnName("provider").HasMaxLength(100);
        builder.Property(e => e.PolicyNumber).HasColumnName("policy_number").HasMaxLength(50);
        builder.Property(e => e.CoverageType).HasColumnName("coverage_type").HasMaxLength(50);
        builder.Property(e => e.Premium).HasColumnName("premium").HasPrecision(18, 4);
        builder.Property(e => e.Currency).HasColumnName("currency").HasMaxLength(10);
        builder.Property(e => e.StartDate).HasColumnName("start_date");
        builder.Property(e => e.EndDate).HasColumnName("end_date");
        builder.Property(e => e.BeneficiaryName).HasColumnName("beneficiary_name").HasMaxLength(100);
        builder.Property(e => e.BeneficiaryPhone).HasColumnName("beneficiary_phone").HasMaxLength(50);
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(20);

        builder.Property(e => e.CreatedOnUtc).HasColumnName("created_on_utc");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
    }
}