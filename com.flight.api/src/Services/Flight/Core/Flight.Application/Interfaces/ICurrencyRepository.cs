using Flight.Application.Dtos;

namespace Flight.Application.Interfaces;

/// <summary>
/// Repository cho currencies — đọc từ bảng currencies (không qua EF aggregate).
/// Includes read, CRUD, and sync operations.
/// </summary>
public interface ICurrencyRepository
{
    // ── Read ──
    Task<IEnumerable<CurrencyDto>> GetAllActiveAsync(CancellationToken ct = default);
    Task<CurrencyDto?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<IEnumerable<CurrencyDto>> GetCurrenciesAsync(CancellationToken ct = default);

    // ── Admin CRUD ──
    Task<bool> UpdateCurrencyAsync(string code, string? name, decimal? rate, string? symbol, decimal? roundUnit, bool? locked, bool? active, CancellationToken ct = default);
    Task<bool> CreateCurrencyAsync(string code, string? name, decimal rate, string? symbol, decimal roundUnit, bool locked, bool active, CancellationToken ct = default);

    // ── Sync ──
    Task<CurrencySyncResult> SyncCurrenciesAsync(Dictionary<string, decimal> rates, decimal vndRate, CancellationToken ct = default);
}

/// <summary>Result of currency sync operation from OpenExchangeRates.</summary>
public sealed class CurrencySyncResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = default!;
    public int CountOk { get; init; }
    public int CountFail { get; init; }
}
