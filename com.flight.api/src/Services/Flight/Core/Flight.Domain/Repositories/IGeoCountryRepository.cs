using Flight.Domain.Aggregates.Geo;

namespace Flight.Domain.Repositories;

public interface IGeoCountryRepository
{
    Task<IEnumerable<GeoCountry>> GetAllAsync(CancellationToken ct = default);
    Task<GeoCountry?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<IEnumerable<GeoCountry>> GetByContinentAsync(string continentCode, CancellationToken ct = default);
    Task AddAsync(GeoCountry entity, CancellationToken ct = default);
    void Update(GeoCountry entity);
    void Delete(GeoCountry entity);
}