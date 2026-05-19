using Flight.Domain.Aggregates.Geo;
using Flight.Domain.Repositories;
using Flight.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Flight.Infrastructure.Persistence.Repositories;

public sealed class GeoContinentRepository(ApplicationDbContext db) : IGeoContinentRepository
{
    public async Task<IEnumerable<GeoContinent>> GetAllAsync(CancellationToken ct = default)
        => await db.GeoContinents
            .Where(c => c.DeletedAt == null)
            .OrderBy(c => c.Id)
            .ToListAsync(ct);

    public async Task<GeoContinent?> GetByCodeAsync(string code, CancellationToken ct = default)
        => await db.GeoContinents
            .FirstOrDefaultAsync(c => c.Id == code && c.DeletedAt == null, ct);

    public async Task AddAsync(GeoContinent entity, CancellationToken ct = default)
        => await db.GeoContinents.AddAsync(entity, ct);

    public void Update(GeoContinent entity)
        => db.GeoContinents.Update(entity);

    public void Delete(GeoContinent entity)
        => db.GeoContinents.Remove(entity);
}