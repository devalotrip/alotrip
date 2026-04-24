using Flight.Domain.Aggregates.Geo;
using Flight.Domain.Repositories;
using Flight.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Flight.Infrastructure.Persistence.Repositories;

public sealed class GeoCountryRepository(ApplicationDbContext db) : IGeoCountryRepository
{
    public async Task<IEnumerable<GeoCountry>> GetAllAsync(CancellationToken ct = default)
        => await db.GeoCountries
            .Where(c => c.DeletedAt == null)
            .OrderBy(c => c.Id)
            .ToListAsync(ct);

    public async Task<GeoCountry?> GetByCodeAsync(string code, CancellationToken ct = default)
        => await db.GeoCountries
            .FirstOrDefaultAsync(c => c.Id == code && c.DeletedAt == null, ct);

    public async Task<IEnumerable<GeoCountry>> GetByContinentAsync(string continentCode, CancellationToken ct = default)
        => await db.GeoCountries
            .Where(c => c.ContinentCode == continentCode && c.DeletedAt == null)
            .OrderBy(c => c.Id)
            .ToListAsync(ct);

    public async Task AddAsync(GeoCountry entity, CancellationToken ct = default)
        => await db.GeoCountries.AddAsync(entity, ct);

    public void Update(GeoCountry entity)
        => db.GeoCountries.Update(entity);

    public void Delete(GeoCountry entity)
        => db.GeoCountries.Remove(entity);
}