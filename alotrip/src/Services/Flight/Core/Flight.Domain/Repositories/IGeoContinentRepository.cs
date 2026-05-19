using Flight.Domain.Aggregates.Geo;

namespace Flight.Domain.Repositories;

public interface IGeoContinentRepository
{
    Task<IEnumerable<GeoContinent>> GetAllAsync(CancellationToken ct = default);
    Task<GeoContinent?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task AddAsync(GeoContinent entity, CancellationToken ct = default);
    void Update(GeoContinent entity);
    void Delete(GeoContinent entity);
}