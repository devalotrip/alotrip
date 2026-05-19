using Flight.Domain.Aggregates.Booking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flight.Infrastructure.Persistence.Configurations;

public sealed class TripTourConfiguration : IEntityTypeConfiguration<TripTourEntity>
{
    public void Configure(EntityTypeBuilder<TripTourEntity> builder)
    {
        builder.ToTable("trip_tours");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(e => e.BookingId).HasColumnName("booking_id");
        builder.Property(e => e.TourName).HasColumnName("tour_name").HasMaxLength(200);
        builder.Property(e => e.TourCode).HasColumnName("tour_code").HasMaxLength(50);
        builder.Property(e => e.DepartureDate).HasColumnName("departure_date");
        builder.Property(e => e.ReturnDate).HasColumnName("return_date");
        builder.Property(e => e.Destination).HasColumnName("destination").HasMaxLength(200);
        builder.Property(e => e.PaxCount).HasColumnName("pax_count");
        builder.Property(e => e.UnitPrice).HasColumnName("unit_price").HasPrecision(18, 4);
        builder.Property(e => e.TotalAmount).HasColumnName("total_amount").HasPrecision(18, 4);
        builder.Property(e => e.Currency).HasColumnName("currency").HasMaxLength(10);
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(20);

        builder.Property(e => e.CreatedOnUtc).HasColumnName("created_on_utc");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
    }
}