using Flight.Application.Dtos;

namespace Flight.Application.Interfaces;

/// <summary>
/// Cache cho kết quả tìm kiếm chuyến bay.
/// Infrastructure implement bằng Redis (thay thế Redis.cs static cũ).
/// </summary>
public interface IFlightCacheService
{
    Task<IEnumerable<FareDataDto>?> GetSearchResultAsync(string cacheKey, CancellationToken ct = default);
    Task SetSearchResultAsync(string cacheKey, IEnumerable<FareDataDto> fares, TimeSpan? ttl = null, CancellationToken ct = default);

    Task<FareDataDto?> GetFareDataAsync(string fareId, CancellationToken ct = default);
    Task SetFareDataAsync(string fareId, FareDataDto fare, TimeSpan? ttl = null, CancellationToken ct = default);
    Task RemoveFareDataAsync(string fareId, CancellationToken ct = default);

    // ── Min fare calendar ────────────────────────────────────────────────────
    // Replaces old tblCache / GetCacheInMonth / GetCacheByListMonth.

    /// <summary>
    /// Gets the cached min fare entry for a specific route + departure date.
    /// </summary>
    Task<MinFareEntryDto?> GetMinFareEntryAsync(string origin, string destination, DateTime date, CancellationToken ct = default);

    /// <summary>
    /// Upserts a min fare entry (only if cheaper than existing or entry expired).
    /// </summary>
    Task SetMinFareEntryAsync(MinFareEntryDto entry, CancellationToken ct = default);

    /// <summary>
    /// Returns all cached daily min fares for a given month (from today onward).
    /// Matches old GetCacheInMonth — daily fare calendar.
    /// </summary>
    Task<List<MinFareEntryDto>> GetMinFaresForMonthAsync(
        string origin, string destination, int year, int month, CancellationToken ct = default);
}
