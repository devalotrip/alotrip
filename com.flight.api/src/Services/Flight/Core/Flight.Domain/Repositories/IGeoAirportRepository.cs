using Flight.Domain.Aggregates.Geo;

namespace Flight.Domain.Repositories;

public interface IGeoAirportRepository
{
    Task<IEnumerable<GeoAirport>> GetAllAsync(CancellationToken ct = default);
    Task<GeoAirport?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<IEnumerable<GeoAirport>> GetByCityAsync(string cityCode, CancellationToken ct = default);
    Task AddAsync(GeoAirport entity, CancellationToken ct = default);
    void Update(GeoAirport entity);
    void Delete(GeoAirport entity);

    /// <summary>
    /// Maps airport IATA codes to continent codes via geo hierarchy:
    /// GeoAirport → GeoCity → GeoCountry.ContinentCode.
    /// Used by commission matching to resolve route regions.
    /// </summary>
    Task<Dictionary<string, string>> GetContinentCodesAsync(IEnumerable<string> airportCodes, CancellationToken ct = default);

    /// <summary>
    /// Maps airport IATA codes to ISO-3166 country codes via geo hierarchy:
    /// GeoAirport → GeoCity → GeoCountry.Id.
    /// Used by search analytics to determine domestic vs international flights.
    /// </summary>
    Task<Dictionary<string, string>> GetCountryCodesAsync(IEnumerable<string> airportCodes, CancellationToken ct = default);
}