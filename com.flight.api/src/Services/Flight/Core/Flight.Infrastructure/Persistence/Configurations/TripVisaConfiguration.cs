using Flight.Domain.Aggregates.Booking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flight.Infrastructure.Persistence.Configurations;

public sealed class TripVisaConfiguration : IEntityTypeConfiguration<TripVisaEntity>
{
    public void Configure(EntityTypeBuilder<TripVisaEntity> builder)
    {
        builder.ToTable("trip_visas");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.BookingId).HasColumnName("booking_id");
        builder.Property(e => e.Code).HasColumnName("code").HasMaxLength(50);
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(200);
        builder.Property(e => e.Price).HasColumnName("price").HasPrecision(18, 4);
        builder.Property(e => e.Value).HasColumnName("value").HasMaxLength(500);
        builder.Property(e => e.Currency).HasColumnName("currency").HasMaxLength(10);
        builder.Property(e => e.PriceVn).HasColumnName("price_vn").HasPrecision(18, 4);

        builder.Property(e => e.CreatedOnUtc).HasColumnName("created_on_utc");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
    }
}