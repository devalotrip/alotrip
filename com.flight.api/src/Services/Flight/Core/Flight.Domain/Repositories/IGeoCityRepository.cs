using Flight.Domain.Aggregates.Geo;

namespace Flight.Domain.Repositories;

public interface IGeoCityRepository
{
    Task<IEnumerable<GeoCity>> GetAllAsync(CancellationToken ct = default);
    Task<GeoCity?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<IEnumerable<GeoCity>> GetByCountryAsync(string countryCode, CancellationToken ct = default);
    Task AddAsync(GeoCity entity, CancellationToken ct = default);
    void Update(GeoCity entity);
    void Delete(GeoCity entity);
}