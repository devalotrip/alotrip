using Flight.Domain.Aggregates.Geo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flight.Infrastructure.Persistence.Configurations;

public sealed class GeoCityConfiguration : IEntityTypeConfiguration<GeoCity>
{
    public void Configure(EntityTypeBuilder<GeoCity> builder)
    {
        builder.ToTable("geo_cities");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.CountryCode).HasColumnName("country_code").HasMaxLength(10);
        builder.Property(e => e.NameVi).HasColumnName("name_vi").HasMaxLength(150);
        builder.Property(e => e.NameEn).HasColumnName("name_en").HasMaxLength(150);
        builder.Property(e => e.NameFr).HasColumnName("name_fr").HasMaxLength(150);
        builder.Property(e => e.Location).HasColumnName("location").HasMaxLength(50);
        builder.Property(e => e.SearchKeys).HasColumnName("search_keys");
        builder.Property(e => e.Visible).HasColumnName("visible").HasDefaultValue(true);

        builder.Property(e => e.CreatedOnUtc).HasColumnName("created_on_utc");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(250);
        builder.Property(e => e.LastModifiedOnUtc).HasColumnName("last_modified_on_utc");
        builder.Property(e => e.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(250);
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
        builder.Property(e => e.DeletedBy).HasColumnName("deleted_by").HasMaxLength(250);

        builder.HasOne(e => e.Country)
            .WithMany()
            .HasForeignKey(e => e.CountryCode)
            .OnDelete(DeleteBehavior.Restrict);
    }
}