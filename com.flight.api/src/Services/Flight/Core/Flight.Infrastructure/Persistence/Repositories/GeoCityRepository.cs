using Flight.Domain.Aggregates.Geo;
using Flight.Domain.Repositories;
using Flight.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Flight.Infrastructure.Persistence.Repositories;

public sealed class GeoCityRepository(ApplicationDbContext db) : IGeoCityRepository
{
    public async Task<IEnumerable<GeoCity>> GetAllAsync(CancellationToken ct = default)
        => await db.GeoCities
            .Where(c => c.DeletedAt == null)
            .OrderBy(c => c.Id)
            .ToListAsync(ct);

    public async Task<GeoCity?> GetByCodeAsync(string code, CancellationToken ct = default)
        => await db.GeoCities
            .FirstOrDefaultAsync(c => c.Id == code && c.DeletedAt == null, ct);

    public async Task<IEnumerable<GeoCity>> GetByCountryAsync(string countryCode, CancellationToken ct = default)
        => await db.GeoCities
            .Where(c => c.CountryCode == countryCode && c.DeletedAt == null)
            .OrderBy(c => c.Id)
            .ToListAsync(ct);

    public async Task AddAsync(GeoCity entity, CancellationToken ct = default)
        => await db.GeoCities.AddAsync(entity, ct);

    public void Update(GeoCity entity)
        => db.GeoCities.Update(entity);

    public void Delete(GeoCity entity)
        => db.GeoCities.Remove(entity);
}