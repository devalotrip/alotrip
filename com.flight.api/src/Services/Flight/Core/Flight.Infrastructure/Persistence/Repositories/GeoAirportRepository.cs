using Flight.Domain.Aggregates.Geo;
using Flight.Domain.Repositories;
using Flight.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Flight.Infrastructure.Persistence.Repositories;

public sealed class GeoAirportRepository(ApplicationDbContext db) : IGeoAirportRepository
{
    public async Task<IEnumerable<GeoAirport>> GetAllAsync(CancellationToken ct = default)
        => await db.GeoAirports
            .Where(a => a.DeletedAt == null)
            .OrderBy(a => a.Id)
            .ToListAsync(ct);

    public async Task<GeoAirport?> GetByCodeAsync(string code, CancellationToken ct = default)
        => await db.GeoAirports
            .FirstOrDefaultAsync(a => a.Id == code && a.DeletedAt == null, ct);

    public async Task<IEnumerable<GeoAirport>> GetByCityAsync(string cityCode, CancellationToken ct = default)
        => await db.GeoAirports
            .Where(a => a.CityCode == cityCode && a.DeletedAt == null)
            .OrderBy(a => a.Id)
            .ToListAsync(ct);

    public async Task AddAsync(GeoAirport entity, CancellationToken ct = default)
        => await db.GeoAirports.AddAsync(entity, ct);

    public void Update(GeoAirport entity)
        => db.GeoAirports.Update(entity);

    public void Delete(GeoAirport entity)
        => db.GeoAirports.Remove(entity);

    /// <inheritdoc />
    public async Task<Dictionary<string, string>> GetContinentCodesAsync(
        IEnumerable<string> airportCodes, CancellationToken ct = default)
    {
        var codes = airportCodes.ToList();
        if (codes.Count == 0)
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // EF translates navigation chain into SQL JOINs:
        // geo_airports → geo_cities → geo_countries.continent_code
        return await db.GeoAirports
            .Where(a => codes.Contains(a.Id)
                     && a.DeletedAt == null
                     && a.City != null
                     && a.City.Country != null)
            .Select(a => new { AirportCode = a.Id, a.City!.Country!.ContinentCode })
            .ToDictionaryAsync(
                x => x.AirportCode,
                x => x.ContinentCode,
                StringComparer.OrdinalIgnoreCase,
                ct);
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, string>> GetCountryCodesAsync(
        IEnumerable<string> airportCodes, CancellationToken ct = default)
    {
        var codes = airportCodes.ToList();
        if (codes.Count == 0)
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // geo_airports → geo_cities → geo_countries.id (ISO country code)
        return await db.GeoAirports
            .Where(a => codes.Contains(a.Id)
                     && a.DeletedAt == null
                     && a.City != null
                     && a.City.Country != null)
            .Select(a => new { AirportCode = a.Id, CountryCode = a.City!.Country!.Id })
            .ToDictionaryAsync(
                x => x.AirportCode,
                x => x.CountryCode,
                StringComparer.OrdinalIgnoreCase,
                ct);
    }
}