using Flight.Domain.Aggregates.Booking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flight.Infrastructure.Persistence.Configurations;

public sealed class PassengerConfiguration : IEntityTypeConfiguration<PassengerEntity>
{
    public void Configure(EntityTypeBuilder<PassengerEntity> builder)
    {
        builder.ToTable("passengers");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.BookingId).HasColumnName("booking_id");
        builder.Property(p => p.FirstName).HasColumnName("first_name").HasMaxLength(50).IsRequired();
        builder.Property(p => p.LastName).HasColumnName("last_name").HasMaxLength(50).IsRequired();
        builder.Property(p => p.MiddleName).HasColumnName("middle_name").HasMaxLength(50);
        builder.Property(p => p.Gender).HasColumnName("gender").HasMaxLength(1).IsRequired();
        builder.Property(p => p.BirthDate).HasColumnName("birth_date");
        builder.Property(p => p.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(10);
        builder.Property(p => p.PassportNo).HasColumnName("passport_no").HasMaxLength(20);
        builder.Property(p => p.PassportExpiry).HasColumnName("passport_expiry");
        builder.Property(p => p.Nationality).HasColumnName("nationality").HasMaxLength(3);
        builder.Property(p => p.BaggageKg).HasColumnName("baggage_kg").HasPrecision(10, 2);
        builder.Property(p => p.FareAmount).HasColumnName("fare_amount").HasPrecision(18, 2);
        builder.Property(p => p.FareCurrency).HasColumnName("fare_currency").HasMaxLength(3);
        builder.Property(p => p.CreatedOnUtc).HasColumnName("created_on_utc");
        builder.Property(p => p.LastModifiedOnUtc).HasColumnName("last_modified_on_utc");
        builder.Property(p => p.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(p => p.BookingId);
    }
}

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<Flight.Domain.Aggregates.Outbox.OutboxMessageEntity>
{
    public void Configure(EntityTypeBuilder<Flight.Domain.Aggregates.Outbox.OutboxMessageEntity> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.EventType).HasMaxLength(300).IsRequired();
        builder.Property(o => o.Payload).IsRequired();
        builder.Property(o => o.ErrorMessage).HasMaxLength(2000);
        builder.Property(o => o.ClaimId).HasMaxLength(50);
        builder.HasIndex(o => new { o.ProcessedOnUtc, o.IsPermanentFail, o.NextAttemptOnUtc });
    }
}
