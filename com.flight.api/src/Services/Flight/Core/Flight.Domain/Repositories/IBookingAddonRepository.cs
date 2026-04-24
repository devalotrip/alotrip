using Flight.Domain.Aggregates.Booking;

namespace Flight.Domain.Repositories;

public interface IBookingAddonRepository
{
    Task SaveChangesAsync(CancellationToken ct = default);

    // Invoice operations
    IQueryable<InvoiceEntity> GetInvoicesQuery();
    Task<InvoiceEntity?> GetInvoiceByIdAsync(int id, CancellationToken ct = default);
    Task AddInvoiceAsync(InvoiceEntity entity, CancellationToken ct = default);

    // Insurance operations
    IQueryable<InsuranceEntity> GetInsurancesQuery();
    Task<InsuranceEntity?> GetInsuranceByIdAsync(int id, CancellationToken ct = default);
    Task AddInsuranceAsync(InsuranceEntity entity, CancellationToken ct = default);

    // CarRental operations
    IQueryable<CarRentalEntity> GetCarRentalsQuery();
    Task<CarRentalEntity?> GetCarRentalByIdAsync(int id, CancellationToken ct = default);
    Task AddCarRentalAsync(CarRentalEntity entity, CancellationToken ct = default);

    // TripTour operations
    IQueryable<TripTourEntity> GetTripToursQuery();
    Task<TripTourEntity?> GetTripTourByIdAsync(int id, CancellationToken ct = default);
    Task AddTripTourAsync(TripTourEntity entity, CancellationToken ct = default);

    // TripVisa operations
    IQueryable<TripVisaEntity> GetTripVisasQuery();
    Task<TripVisaEntity?> GetTripVisaByIdAsync(int id, CancellationToken ct = default);
    Task AddTripVisaAsync(TripVisaEntity entity, CancellationToken ct = default);

    // TripCancellation operations
    IQueryable<TripCancellationEntity> GetTripCancellationsQuery();
    Task<TripCancellationEntity?> GetTripCancellationByIdAsync(int id, CancellationToken ct = default);
    Task AddTripCancellationAsync(TripCancellationEntity entity, CancellationToken ct = default);
}
