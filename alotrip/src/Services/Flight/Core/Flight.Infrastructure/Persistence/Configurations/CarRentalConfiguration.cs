using Flight.Domain.Aggregates.Booking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flight.Infrastructure.Persistence.Configurations;

public sealed class CarRentalConfiguration : IEntityTypeConfiguration<CarRentalEntity>
{
    public void Configure(EntityTypeBuilder<CarRentalEntity> builder)
    {
        builder.ToTable("car_rentals");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(e => e.BookingId).HasColumnName("booking_id");
        builder.Property(e => e.Provider).HasColumnName("provider").HasMaxLength(100);
        builder.Property(e => e.PickupLocation).HasColumnName("pickup_location").HasMaxLength(200);
        builder.Property(e => e.DropoffLocation).HasColumnName("dropoff_location").HasMaxLength(200);
        builder.Property(e => e.PickupDate).HasColumnName("pickup_date");
        builder.Property(e => e.DropoffDate).HasColumnName("dropoff_date");
        builder.Property(e => e.CarType).HasColumnName("car_type").HasMaxLength(100);
        builder.Property(e => e.CarModel).HasColumnName("car_model").HasMaxLength(100);
        builder.Property(e => e.DailyRate).HasColumnName("daily_rate").HasPrecision(18, 4);
        builder.Property(e => e.TotalDays).HasColumnName("total_days");
        builder.Property(e => e.TotalAmount).HasColumnName("total_amount").HasPrecision(18, 4);
        builder.Property(e => e.Currency).HasColumnName("currency").HasMaxLength(10);
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(20);

        builder.Property(e => e.CreatedOnUtc).HasColumnName("created_on_utc");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
    }
}