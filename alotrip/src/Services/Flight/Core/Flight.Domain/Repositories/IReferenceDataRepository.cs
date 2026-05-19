using Flight.Domain.Aggregates.Booking;

namespace Flight.Domain.Repositories;

public interface IReferenceDataRepository
{
    Task SaveChangesAsync(CancellationToken ct = default);

    // Airline operations
    IQueryable<AirlineEntity> GetAirlinesQuery();
    Task<AirlineEntity?> GetAirlineByIdAsync(int id, CancellationToken ct = default);
    Task AddAirlineAsync(AirlineEntity entity, CancellationToken ct = default);

    // Aircraft operations
    IQueryable<AircraftEntity> GetAircraftsQuery();
    Task<AircraftEntity?> GetAircraftByIdAsync(int id, CancellationToken ct = default);
    Task AddAircraftAsync(AircraftEntity entity, CancellationToken ct = default);

    // AirlineType operations
    IQueryable<AirlineTypeEntity> GetAirlineTypesQuery();
    Task<AirlineTypeEntity?> GetAirlineTypeByIdAsync(int id, CancellationToken ct = default);
    Task AddAirlineTypeAsync(AirlineTypeEntity entity, CancellationToken ct = default);

    // PassengerType operations
    IQueryable<PassengerTypeEntity> GetPassengerTypesQuery();
    Task<PassengerTypeEntity?> GetPassengerTypeByCodeAsync(string code, CancellationToken ct = default);
    Task AddPassengerTypeAsync(PassengerTypeEntity entity, CancellationToken ct = default);
    void RemovePassengerType(PassengerTypeEntity entity);

    // Baggage operations
    IQueryable<BaggageEntity> GetBaggagesQuery();
    Task<BaggageEntity?> GetBaggageByIdAsync(int id, CancellationToken ct = default);
    Task AddBaggageAsync(BaggageEntity entity, CancellationToken ct = default);

    // ClassNote operations
    IQueryable<ClassAndNoteEntity> GetClassNotesQuery();
    Task<ClassAndNoteEntity?> GetClassNoteByIdAsync(int id, CancellationToken ct = default);
    Task AddClassNoteAsync(ClassAndNoteEntity entity, CancellationToken ct = default);
}
