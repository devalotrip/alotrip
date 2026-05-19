using Flight.Domain.Aggregates.Booking;
using Flight.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flight.Infrastructure.Persistence.Configurations;

public sealed class BookingConfiguration : IEntityTypeConfiguration<BookingEntity>
{
    public void Configure(EntityTypeBuilder<BookingEntity> builder)
    {
        builder.ToTable("bookings");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id).HasColumnName("id");
        builder.Property(b => b.BookingCode).HasColumnName("booking_code").HasMaxLength(20).IsRequired();
        builder.Property(b => b.AgentCode).HasColumnName("agent_code").HasMaxLength(20).IsRequired();
        builder.Property(b => b.Source).HasColumnName("source").HasConversion<string>().HasMaxLength(20);
        builder.Property(b => b.TripType).HasColumnName("trip_type").HasConversion<string>().HasMaxLength(20);
        builder.Property(b => b.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        builder.Property(b => b.Origin).HasColumnName("origin").HasMaxLength(3).IsRequired();
        builder.Property(b => b.Destination).HasColumnName("destination").HasMaxLength(3).IsRequired();
        builder.Property(b => b.DepartDate).HasColumnName("depart_date");
        builder.Property(b => b.ReturnDate).HasColumnName("return_date");
        builder.Property(b => b.TotalAmount).HasColumnName("total_amount").HasPrecision(18, 2);
        builder.Property(b => b.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        builder.Property(b => b.ServiceFee).HasColumnName("service_fee").HasPrecision(18, 2);
        builder.Property(b => b.ContactName).HasColumnName("contact_name").HasMaxLength(100);
        builder.Property(b => b.ContactEmail).HasColumnName("contact_email").HasMaxLength(150);
        builder.Property(b => b.ContactPhone).HasColumnName("contact_phone").HasMaxLength(20);
        builder.Property(b => b.SessionId).HasColumnName("session_id").HasMaxLength(500);
        builder.Property(b => b.PccCode).HasColumnName("pcc_code").HasMaxLength(10);
        builder.Property(b => b.FareId).HasColumnName("fare_id").HasMaxLength(100);
        builder.Property(b => b.ExpiresAt).HasColumnName("expires_at");
        builder.Property(b => b.CreatedOnUtc).HasColumnName("created_on_utc");
        builder.Property(b => b.LastModifiedOnUtc).HasColumnName("last_modified_on_utc");
        builder.Property(b => b.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(b => b.BookingCode).IsUnique();
        builder.HasIndex(b => b.AgentCode);
        builder.HasIndex(b => b.Status);
        builder.HasIndex(b => b.CreatedOnUtc);

        builder.HasMany(b => b.Flights)
            .WithOne()
            .HasForeignKey(f => f.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(b => b.Passengers)
            .WithOne()
            .HasForeignKey(p => p.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(b => b.Tickets)
            .WithOne()
            .HasForeignKey(t => t.BookingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class BookingFlightConfiguration : IEntityTypeConfiguration<BookingFlightEntity>
{
    public void Configure(EntityTypeBuilder<BookingFlightEntity> builder)
    {
        builder.ToTable("booking_flights");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id).HasColumnName("id");
        builder.Property(f => f.BookingId).HasColumnName("booking_id");
        builder.Property(f => f.Origin).HasColumnName("origin").HasMaxLength(3).IsRequired();
        builder.Property(f => f.Destination).HasColumnName("destination").HasMaxLength(3).IsRequired();
        builder.Property(f => f.DepartTime).HasColumnName("depart_time");
        builder.Property(f => f.ArriveTime).HasColumnName("arrive_time");
        builder.Property(f => f.Airline).HasColumnName("airline").HasMaxLength(10).IsRequired();
        builder.Property(f => f.StopCount).HasColumnName("stop_count");
        builder.Property(f => f.CreatedOnUtc).HasColumnName("created_on_utc");
        builder.Property(f => f.LastModifiedOnUtc).HasColumnName("last_modified_on_utc");
        builder.Property(f => f.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(f => f.BookingId);

        builder.HasMany(f => f.Segments)
            .WithOne()
            .HasForeignKey(s => s.BookingFlightId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class BookingSegmentConfiguration : IEntityTypeConfiguration<BookingSegmentEntity>
{
    public void Configure(EntityTypeBuilder<BookingSegmentEntity> builder)
    {
        builder.ToTable("booking_segments");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.BookingFlightId).HasColumnName("booking_flight_id");
        builder.Property(s => s.FlightNumber).HasColumnName("flight_number").HasMaxLength(10).IsRequired();
        builder.Property(s => s.Airline).HasColumnName("airline").HasMaxLength(10).IsRequired();
        builder.Property(s => s.Origin).HasColumnName("origin").HasMaxLength(3).IsRequired();
        builder.Property(s => s.Destination).HasColumnName("destination").HasMaxLength(3).IsRequired();
        builder.Property(s => s.DepartTime).HasColumnName("depart_time");
        builder.Property(s => s.ArriveTime).HasColumnName("arrive_time");
        builder.Property(s => s.CabinClass).HasColumnName("cabin_class").HasMaxLength(5).IsRequired();
        builder.Property(s => s.AircraftType).HasColumnName("aircraft_type").HasMaxLength(10);
        builder.Property(s => s.CreatedOnUtc).HasColumnName("created_on_utc");
        builder.Property(s => s.LastModifiedOnUtc).HasColumnName("last_modified_on_utc");
        builder.Property(s => s.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(s => s.BookingFlightId);
    }
}

public sealed class TicketConfiguration : IEntityTypeConfiguration<TicketEntity>
{
    public void Configure(EntityTypeBuilder<TicketEntity> builder)
    {
        builder.ToTable("tickets");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id).HasColumnName("id");
        builder.Property(t => t.BookingId).HasColumnName("booking_id");
        builder.Property(t => t.PassengerId).HasColumnName("passenger_id");
        builder.Property(t => t.TicketNumber).HasColumnName("ticket_number").HasMaxLength(30).IsRequired();
        builder.Property(t => t.PassengerName).HasColumnName("passenger_name").HasMaxLength(100);
        builder.Property(t => t.Airline).HasColumnName("airline").HasMaxLength(10);
        builder.Property(t => t.IssuedAt).HasColumnName("issued_at");
        builder.Property(t => t.CreatedOnUtc).HasColumnName("created_on_utc");
        builder.Property(t => t.LastModifiedOnUtc).HasColumnName("last_modified_on_utc");
        builder.Property(t => t.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(t => t.TicketNumber).IsUnique();
        builder.HasIndex(t => t.BookingId);
    }
}
