using Flight.Application.Dtos;
using Flight.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Flight.Infrastructure.Cache;

/// <summary>
/// Redis implementation của IFlightCacheService.
/// Thay thế Redis.cs static class trong source cũ.
/// </summary>
public sealed class RedisFlightCacheService(IDistributedCache cache) : IFlightCacheService
{
    private static readonly DistributedCacheEntryOptions _searchOpts = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
    };
    private static readonly DistributedCacheEntryOptions _fareOpts = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
    };
    private static readonly DistributedCacheEntryOptions _minFareOpts = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
    };

    public async Task<IEnumerable<FareDataDto>?> GetSearchResultAsync(
        string cacheKey, CancellationToken ct = default)
    {
        var json = await cache.GetStringAsync(cacheKey, ct);
        return json is null ? null : JsonSerializer.Deserialize<List<FareDataDto>>(json);
    }

    public async Task SetSearchResultAsync(
        string cacheKey, IEnumerable<FareDataDto> fares,
        TimeSpan? ttl = null, CancellationToken ct = default)
    {
        var opts = ttl.HasValue
            ? new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl }
            : _searchOpts;
        await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(fares), opts, ct);
    }

    public async Task<FareDataDto?> GetFareDataAsync(string fareId, CancellationToken ct = default)
    {
        var json = await cache.GetStringAsync($"fare:{fareId}", ct);
        return json is null ? null : JsonSerializer.Deserialize<FareDataDto>(json);
    }

    public async Task SetFareDataAsync(string fareId, FareDataDto fare,
        TimeSpan? ttl = null, CancellationToken ct = default)
    {
        var opts = ttl.HasValue
            ? new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl }
            : _fareOpts;
        await cache.SetStringAsync($"fare:{fareId}", JsonSerializer.Serialize(fare), opts, ct);
    }

    public async Task RemoveFareDataAsync(string fareId, CancellationToken ct = default)
        => await cache.RemoveAsync($"fare:{fareId}", ct);

    // ── Min fare calendar ─────────────────────────────────────────────────────

    public async Task<MinFareEntryDto?> GetMinFareEntryAsync(
        string origin, string destination, DateTime date, CancellationToken ct = default)
    {
        var key = BuildMinFareKey(origin, destination, date);
        var json = await cache.GetStringAsync(key, ct);
        return json is null ? null : JsonSerializer.Deserialize<MinFareEntryDto>(json);
    }

    public async Task SetMinFareEntryAsync(MinFareEntryDto entry, CancellationToken ct = default)
    {
        var key = BuildMinFareKey(entry.Origin, entry.Destination, entry.DepartDate);
        await cache.SetStringAsync(key, JsonSerializer.Serialize(entry), _minFareOpts, ct);
    }

    public async Task<List<MinFareEntryDto>> GetMinFaresForMonthAsync(
        string origin, string destination, int year, int month, CancellationToken ct = default)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var today = DateTime.Today;
        var tasks = new List<Task<MinFareEntryDto?>>();

        for (int day = 1; day <= daysInMonth; day++)
        {
            var date = new DateTime(year, month, day);
            if (date >= today)
                tasks.Add(GetMinFareEntryAsync(origin, destination, date, ct));
        }

        var entries = await Task.WhenAll(tasks);
        return entries.Where(e => e is not null).Cast<MinFareEntryDto>().ToList();
    }

    private static string BuildMinFareKey(string origin, string destination, DateTime date)
        => $"minfare:{origin}:{destination}:{date:yyyyMMdd}";
}
