using Flight.Domain.Aggregates.Booking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flight.Infrastructure.Persistence.Configurations;

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<InvoiceEntity>
{
    public void Configure(EntityTypeBuilder<InvoiceEntity> builder)
    {
        builder.ToTable("invoices");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(e => e.BookingId).HasColumnName("booking_id");
        builder.Property(e => e.CompanyName).HasColumnName("company_name").HasMaxLength(150);
        builder.Property(e => e.Address).HasColumnName("address").HasMaxLength(250);
        builder.Property(e => e.CityName).HasColumnName("city_name").HasMaxLength(100);
        builder.Property(e => e.TaxCode).HasColumnName("tax_code").HasMaxLength(50);
        builder.Property(e => e.Receiver).HasColumnName("receiver").HasMaxLength(100);
        builder.Property(e => e.ReceiverPhone).HasColumnName("receiver_phone").HasMaxLength(50);
        builder.Property(e => e.ReceiverEmail).HasColumnName("receiver_email").HasMaxLength(150);
        builder.Property(e => e.TotalAmount).HasColumnName("total_amount").HasPrecision(18, 4);
        builder.Property(e => e.Currency).HasColumnName("currency").HasMaxLength(10);
        builder.Property(e => e.InvoiceDate).HasColumnName("invoice_date");
        builder.Property(e => e.InvoiceNumber).HasColumnName("invoice_number").HasMaxLength(50);

        builder.Property(e => e.CreatedOnUtc).HasColumnName("created_on_utc");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(250);
        builder.Property(e => e.LastModifiedOnUtc).HasColumnName("last_modified_on_utc");
        builder.Property(e => e.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(250);
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
        builder.Property(e => e.DeletedBy).HasColumnName("deleted_by").HasMaxLength(250);
    }
}