using Flight.Domain.Aggregates.Booking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flight.Infrastructure.Persistence.Configurations;

public sealed class TripCancellationConfiguration : IEntityTypeConfiguration<TripCancellationEntity>
{
    public void Configure(EntityTypeBuilder<TripCancellationEntity> builder)
    {
        builder.ToTable("trip_cancellations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.BookingId).HasColumnName("booking_id");
        builder.Property(e => e.MarkupAmount).HasColumnName("markup_amount").HasPrecision(18, 4);
        builder.Property(e => e.MarkupPercent).HasColumnName("markup_percent").HasPrecision(18, 4);
        builder.Property(e => e.Price).HasColumnName("price").HasPrecision(18, 4);
        builder.Property(e => e.Currency).HasColumnName("currency").HasMaxLength(10);
        builder.Property(e => e.BookingPrice).HasColumnName("booking_price").HasPrecision(18, 4);
        builder.Property(e => e.Value).HasColumnName("value").HasMaxLength(500);

        builder.Property(e => e.CreatedOnUtc).HasColumnName("created_on_utc");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
    }
}

public sealed class AircraftConfiguration : IEntityTypeConfiguration<AircraftEntity>
{
    public void Configure(EntityTypeBuilder<AircraftEntity> builder)
    {
        builder.ToTable("aircrafts");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.IATA).HasColumnName("iata").HasMaxLength(10).IsRequired();
        builder.Property(e => e.Manufacturer).HasColumnName("manufacturer").HasMaxLength(100);
        builder.Property(e => e.Model).HasColumnName("model").HasMaxLength(100);
        builder.Property(e => e.Visible).HasColumnName("visible");

        builder.Property(e => e.CreatedOnUtc).HasColumnName("created_on_utc");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
    }
}

public sealed class AirlineConfiguration : IEntityTypeConfiguration<AirlineEntity>
{
    public void Configure(EntityTypeBuilder<AirlineEntity> builder)
    {
        builder.ToTable("airlines");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Code).HasColumnName("code").HasMaxLength(10).IsRequired();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(200);
        builder.Property(e => e.Logo).HasColumnName("logo").HasMaxLength(500);
        builder.Property(e => e.Visible).HasColumnName("visible");

        builder.HasIndex(e => e.Code).IsUnique();

        builder.Property(e => e.CreatedOnUtc).HasColumnName("created_on_utc");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
    }
}

public sealed class AirlineTypeConfiguration : IEntityTypeConfiguration<AirlineTypeEntity>
{
    public void Configure(EntityTypeBuilder<AirlineTypeEntity> builder)
    {
        builder.ToTable("airline_types");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Code).HasColumnName("code").HasMaxLength(10).IsRequired();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(200);
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(e => e.Visible).HasColumnName("visible");

        builder.HasIndex(e => e.Code).IsUnique();

        builder.Property(e => e.CreatedOnUtc).HasColumnName("created_on_utc");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
    }
}

public sealed class BaggageConfiguration : IEntityTypeConfiguration<BaggageEntity>
{
    public void Configure(EntityTypeBuilder<BaggageEntity> builder)
    {
        builder.ToTable("baggages");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.BaggageCode).HasColumnName("baggage_code").HasMaxLength(50);
        builder.Property(e => e.FlightId).HasColumnName("flight_id");
        builder.Property(e => e.FlightNumber).HasColumnName("flight_number").HasMaxLength(20);
        builder.Property(e => e.PaxId).HasColumnName("pax_id");
        builder.Property(e => e.BookingId).HasColumnName("booking_id");
        builder.Property(e => e.Weight).HasColumnName("weight");
        builder.Property(e => e.WeightUnit).HasColumnName("weight_unit");
        builder.Property(e => e.PieceAllowance).HasColumnName("piece_allowance");
        builder.Property(e => e.CabinClass).HasColumnName("cabin_class").HasMaxLength(20);
        builder.Property(e => e.BaggageType).HasColumnName("baggage_type").HasMaxLength(50);

        builder.Property(e => e.CreatedOnUtc).HasColumnName("created_on_utc");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
    }
}

public sealed class PartnerConfiguration : IEntityTypeConfiguration<PartnerEntity>
{
    public void Configure(EntityTypeBuilder<PartnerEntity> builder)
    {
        builder.ToTable("partners");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(e => e.Active).HasColumnName("active");

        builder.Property(e => e.CreatedOnUtc).HasColumnName("created_on_utc");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
    }
}

public sealed class AgentPartnerConfiguration : IEntityTypeConfiguration<AgentPartnerEntity>
{
    public void Configure(EntityTypeBuilder<AgentPartnerEntity> builder)
    {
        builder.ToTable("agent_partners");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.AgentId).HasColumnName("agent_id");
        builder.Property(e => e.PartnerId).HasColumnName("partner_id");
        builder.Property(e => e.IgnoredMode).HasColumnName("ignored_mode");
        builder.Property(e => e.ListStartPoint).HasColumnName("list_start_point").HasMaxLength(500);
        builder.Property(e => e.Active).HasColumnName("active");

        builder.Property(e => e.CreatedOnUtc).HasColumnName("created_on_utc");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
    }
}

public sealed class ClassAndNoteConfiguration : IEntityTypeConfiguration<ClassAndNoteEntity>
{
    public void Configure(EntityTypeBuilder<ClassAndNoteEntity> builder)
    {
        builder.ToTable("class_and_notes");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.AirlineCode).HasColumnName("airline_code").HasMaxLength(10).IsRequired();
        builder.Property(e => e.Class).HasColumnName("class_code").HasMaxLength(10).IsRequired();
        builder.Property(e => e.ShowClass).HasColumnName("show_class").HasMaxLength(20);
        builder.Property(e => e.NonRefundable).HasColumnName("non_refundable");
        builder.Property(e => e.Visible).HasColumnName("visible");
        builder.Property(e => e.StartAirportCode).HasColumnName("start_airport_code").HasMaxLength(10);
        builder.Property(e => e.EndAirportCode).HasColumnName("end_airport_code").HasMaxLength(10);
        builder.Property(e => e.StartCityCode).HasColumnName("start_city_code").HasMaxLength(10);
        builder.Property(e => e.EndCityCode).HasColumnName("end_city_code").HasMaxLength(10);
        builder.Property(e => e.StartCountryCode).HasColumnName("start_country_code").HasMaxLength(10);
        builder.Property(e => e.EndCountryCode).HasColumnName("end_country_code").HasMaxLength(10);
        builder.Property(e => e.StartContinentCode).HasColumnName("start_continent_code").HasMaxLength(10);
        builder.Property(e => e.EndContinentCode).HasColumnName("end_continent_code").HasMaxLength(10);

        builder.Property(e => e.CreatedOnUtc).HasColumnName("created_on_utc");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
    }
}

public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRoleEntity>
{
    public void Configure(EntityTypeBuilder<UserRoleEntity> builder)
    {
        builder.ToTable("user_roles");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);

        builder.Property(e => e.CreatedOnUtc).HasColumnName("created_on_utc");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
    }
}

public sealed class SearchAnalyticConfiguration : IEntityTypeConfiguration<SearchAnalyticEntity>
{
    public void Configure(EntityTypeBuilder<SearchAnalyticEntity> builder)
    {
        builder.ToTable("search_analytics");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.AgentCode).HasColumnName("agent_code").HasMaxLength(50);
        builder.Property(e => e.Time).HasColumnName("time");
        builder.Property(e => e.StartPoint).HasColumnName("start_point").HasMaxLength(10);
        builder.Property(e => e.EndPoint).HasColumnName("end_point").HasMaxLength(10);
        builder.Property(e => e.Itinerary).HasColumnName("itinerary");
        builder.Property(e => e.DepartDate).HasColumnName("depart_date");
        builder.Property(e => e.ReturnDate).HasColumnName("return_date");
        builder.Property(e => e.FlightType).HasColumnName("flight_type");
        builder.Property(e => e.IPAddress).HasColumnName("ip_address").HasMaxLength(50);
        builder.Property(e => e.Sources).HasColumnName("sources").HasMaxLength(200);

        builder.Property(e => e.CreatedOnUtc).HasColumnName("created_on_utc");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
    }
}

public sealed class AirlineIgnoreConfiguration : IEntityTypeConfiguration<AirlineIgnoreEntity>
{
    public void Configure(EntityTypeBuilder<AirlineIgnoreEntity> builder)
    {
        builder.ToTable("airline_ignores");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.AgentId).HasColumnName("agent_id");
        builder.Property(e => e.Airline).HasColumnName("airline").HasMaxLength(10).IsRequired();
        builder.Property(e => e.FilterByPlatingCarrier).HasColumnName("filter_by_plating_carrier");
        builder.Property(e => e.FilterByAnySegment).HasColumnName("filter_by_any_segment");
        builder.Property(e => e.FilterByAllSegment).HasColumnName("filter_by_all_segment");

        builder.Property(e => e.CreatedOnUtc).HasColumnName("created_on_utc");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
    }
}

public sealed class AgentPccConfiguration : IEntityTypeConfiguration<AgentPccEntity>
{
    public void Configure(EntityTypeBuilder<AgentPccEntity> builder)
    {
        builder.ToTable("agent_pccs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.AgentId).HasColumnName("agent_id");
        builder.Property(e => e.Pcc).HasColumnName("pcc").HasMaxLength(10).IsRequired();
        builder.Property(e => e.IgnoredMode).HasColumnName("ignored_mode");
        builder.Property(e => e.ListStartPoint).HasColumnName("list_start_point").HasMaxLength(500);
        builder.Property(e => e.Active).HasColumnName("active");

        builder.Property(e => e.CreatedOnUtc).HasColumnName("created_on_utc");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
    }
}

public sealed class PccConfiguration : IEntityTypeConfiguration<PccEntity>
{
    public void Configure(EntityTypeBuilder<PccEntity> builder)
    {
        builder.ToTable("pccs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Active).HasColumnName("active");

        builder.Property(e => e.CreatedOnUtc).HasColumnName("created_on_utc");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
    }
}

public sealed class LccInfoConfiguration : IEntityTypeConfiguration<LccInfoEntity>
{
    public void Configure(EntityTypeBuilder<LccInfoEntity> builder)
    {
        builder.ToTable("lcc_infos");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.AgentId).HasColumnName("agent_id");
        builder.Property(e => e.Airline).HasColumnName("airline").HasMaxLength(10).IsRequired();
        builder.Property(e => e.AllowSearch).HasColumnName("allow_search");
        builder.Property(e => e.AllowBook).HasColumnName("allow_book");
        builder.Property(e => e.ProxyServerId).HasColumnName("proxy_server_id").HasMaxLength(100);
        builder.Property(e => e.ProxyServerBookId).HasColumnName("proxy_server_book_id").HasMaxLength(100);

        builder.Property(e => e.CreatedOnUtc).HasColumnName("created_on_utc");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
    }
}

/// <summary>
/// EF configuration for AgentEntity → "agents" table.
/// Replaces the previous raw-SQL-only approach.
/// </summary>
public sealed class AgentConfiguration : IEntityTypeConfiguration<AgentEntity>
{
    public void Configure(EntityTypeBuilder<AgentEntity> builder)
    {
        builder.ToTable("agents");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(e => e.AgentCode).HasColumnName("agent_code").HasMaxLength(50).IsRequired();
        builder.HasIndex(e => e.AgentCode).IsUnique();

        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        builder.Property(e => e.Address).HasColumnName("address").HasMaxLength(150);
        builder.Property(e => e.Tel).HasColumnName("tel").HasMaxLength(50);
        builder.Property(e => e.Email).HasColumnName("email").HasMaxLength(150).IsRequired();
        builder.Property(e => e.PasswordHash).HasColumnName("password_hash").HasMaxLength(250).IsRequired();

        // LCC flags
        builder.Property(e => e.LccVnActiveDomestic).HasColumnName("lcc_vn_active_domestic");
        builder.Property(e => e.LccVnActiveGlobal).HasColumnName("lcc_vn_active_global");

        // Galileo config
        builder.Property(e => e.GalileoPcc).HasColumnName("galileo_pcc").HasMaxLength(50);
        builder.Property(e => e.GalileoActive).HasColumnName("galileo_active");

        // System settings
        builder.Property(e => e.DefaultCurrency).HasColumnName("default_currency").HasMaxLength(10);
        builder.Property(e => e.EnableCache).HasColumnName("enable_cache");
        builder.Property(e => e.CacheTimeMinutes).HasColumnName("cache_time_minutes");
        builder.Property(e => e.SendMailInApi).HasColumnName("send_mail_in_api");
        builder.Property(e => e.EmailSender).HasColumnName("email_sender");
        builder.Property(e => e.CombinedMode).HasColumnName("combined_mode");

        // Lifecycle
        builder.Property(e => e.ExpiryDate).HasColumnName("expiry_date");
        builder.Property(e => e.Active).HasColumnName("active");

        // Fee markup
        builder.Property(e => e.BaggageFeePercent).HasColumnName("baggage_fee_percent");
        builder.Property(e => e.BaggageFeeAmount).HasColumnName("baggage_fee_amount");

        // Base Entity audit fields
        builder.Property(e => e.CreatedOnUtc).HasColumnName("created_at");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
    }
}

public sealed class PassengerTypeConfiguration : IEntityTypeConfiguration<PassengerTypeEntity>
{
    public void Configure(EntityTypeBuilder<PassengerTypeEntity> builder)
    {
        builder.ToTable("passenger_types");

        builder.HasKey(e => e.Code);
        builder.Property(e => e.Code).HasColumnName("code").HasMaxLength(10);
        builder.Property(e => e.Icon).HasColumnName("icon").HasMaxLength(50);
        builder.Property(e => e.NameVi).HasColumnName("name_vi").HasMaxLength(150);
        builder.Property(e => e.NameEn).HasColumnName("name_en").HasMaxLength(150);
        builder.Property(e => e.NameFr).HasColumnName("name_fr").HasMaxLength(150);
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(150);
    }
}