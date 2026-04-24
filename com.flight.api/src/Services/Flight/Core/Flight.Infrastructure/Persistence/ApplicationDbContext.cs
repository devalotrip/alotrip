using Flight.Domain.Aggregates.Booking;
using Flight.Domain.Aggregates.Geo;
using Flight.Domain.Aggregates.Outbox;
using Flight.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Shared.BuildingBlocks.Abstractions;
using System.Reflection;

namespace Flight.Infrastructure.Persistence;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    AuditableEntityInterceptor auditInterceptor,
    DispatchDomainEventsInterceptor domainEventsInterceptor)
    : DbContext(options), IUnitOfWork
{
    public DbSet<BookingEntity>        Bookings        => Set<BookingEntity>();
    public DbSet<BookingFlightEntity>  BookingFlights  => Set<BookingFlightEntity>();
    public DbSet<BookingSegmentEntity> BookingSegments => Set<BookingSegmentEntity>();
    public DbSet<PassengerEntity>      Passengers      => Set<PassengerEntity>();
    public DbSet<TicketEntity>         Tickets         => Set<TicketEntity>();
    public DbSet<OutboxMessageEntity>  OutboxMessages  => Set<OutboxMessageEntity>();
    public DbSet<InvoiceEntity>        Invoices        => Set<InvoiceEntity>();
    public DbSet<InsuranceEntity>     Insurances      => Set<InsuranceEntity>();
    public DbSet<CarRentalEntity>     CarRentals      => Set<CarRentalEntity>();
    public DbSet<TripTourEntity>      TripTours       => Set<TripTourEntity>();
    public DbSet<TripVisaEntity>      TripVisas       => Set<TripVisaEntity>();
    public DbSet<UserAccountEntity>   UserAccounts    => Set<UserAccountEntity>();
    public DbSet<TripCancellationEntity> TripCancellations => Set<TripCancellationEntity>();
    public DbSet<AircraftEntity>      Aircrafts       => Set<AircraftEntity>();
    public DbSet<AirlineEntity>       Airlines        => Set<AirlineEntity>();
    public DbSet<AirlineTypeEntity>   AirlineTypes     => Set<AirlineTypeEntity>();
    public DbSet<BaggageEntity>       Baggages        => Set<BaggageEntity>();
    public DbSet<PartnerEntity>       Partners        => Set<PartnerEntity>();
    public DbSet<AgentPartnerEntity>   AgentPartners    => Set<AgentPartnerEntity>();
    public DbSet<ClassAndNoteEntity>  ClassAndNotes   => Set<ClassAndNoteEntity>();
    public DbSet<UserRoleEntity>      UserRoles       => Set<UserRoleEntity>();
    public DbSet<SearchAnalyticEntity> SearchAnalytics => Set<SearchAnalyticEntity>();
    public DbSet<AirlineIgnoreEntity> AirlineIgnores  => Set<AirlineIgnoreEntity>();
    public DbSet<AgentPccEntity>      AgentPccs       => Set<AgentPccEntity>();
    public DbSet<PccEntity>           Pccs            => Set<PccEntity>();
    public DbSet<LccInfoEntity>       LccInfos        => Set<LccInfoEntity>();
    public DbSet<AgentEntity>          Agents          => Set<AgentEntity>();
    public DbSet<PassengerTypeEntity>  PassengerTypes  => Set<PassengerTypeEntity>();

    public DbSet<GeoContinent> GeoContinents => Set<GeoContinent>();
    public DbSet<GeoCountry>   GeoCountries  => Set<GeoCountry>();
    public DbSet<GeoCity>      GeoCities     => Set<GeoCity>();
    public DbSet<GeoAirport>  GeoAirports   => Set<GeoAirport>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .AddInterceptors(auditInterceptor, domainEventsInterceptor);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
