using Flight.Application.Dtos;
using Flight.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Flight.Infrastructure.Persistence.Repositories;

/// <summary>
/// Full currency repository — reads, CRUD, and sync from the `currencies` table.
/// Uses raw SQL (no EF entity needed).
/// </summary>
public sealed class CurrencyRepository(ApplicationDbContext db) : ICurrencyRepository
{
    public async Task<IEnumerable<CurrencyDto>> GetAllActiveAsync(CancellationToken ct = default)
    {
        return await db.Database
            .SqlQuery<CurrencyDto>($"""
                SELECT code    AS "Code",
                       name    AS "Name",
                       rate    AS "Rate",
                       symbol  AS "Symbol",
                       round_unit AS "RoundUnit",
                       locked  AS "Locked",
                       active  AS "Active"
                FROM   currencies
                WHERE  active = true
                ORDER  BY code
                """)
            .ToListAsync(ct);
    }

    public async Task<CurrencyDto?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        return await db.Database
            .SqlQuery<CurrencyDto>($"""
                SELECT code    AS "Code",
                       name    AS "Name",
                       rate    AS "Rate",
                       symbol  AS "Symbol",
                       round_unit AS "RoundUnit",
                       locked  AS "Locked",
                       active  AS "Active"
                FROM   currencies
                WHERE  code = {code}
                """)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IEnumerable<CurrencyDto>> GetCurrenciesAsync(CancellationToken ct = default)
    {
        return await db.Database
            .SqlQueryRaw<CurrencyDto>("""
                SELECT code    AS "Code",
                       name    AS "Name",
                       rate    AS "Rate",
                       symbol  AS "Symbol",
                       round_unit AS "RoundUnit",
                       locked  AS "Locked",
                       active  AS "Active"
                FROM   currencies
                ORDER  BY code
                """)
            .ToListAsync(ct);
    }

    public async Task<bool> UpdateCurrencyAsync(
        string code, string? name, decimal? rate, string? symbol,
        decimal? roundUnit, bool? locked, bool? active, CancellationToken ct = default)
    {
        int rows = await db.Database.ExecuteSqlRawAsync("""
            UPDATE currencies SET
                name       = COALESCE({0}, name),
                rate       = COALESCE({1}, rate),
                symbol     = COALESCE({2}, symbol),
                round_unit = COALESCE({3}, round_unit),
                locked     = COALESCE({4}, locked),
                active     = COALESCE({5}, active)
            WHERE code = {6}
            """, new object?[] { name, rate, symbol, roundUnit, locked, active, code.ToUpperInvariant() }!);
        return rows > 0;
    }

    public async Task<bool> CreateCurrencyAsync(
        string code, string? name, decimal rate, string? symbol,
        decimal roundUnit, bool locked, bool active, CancellationToken ct = default)
    {
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO currencies (code, name, rate, symbol, round_unit, locked, active)
            VALUES ({0},{1},{2},{3},{4},{5},{6})
            ON CONFLICT (code) DO NOTHING
            """, new object?[] { code.ToUpperInvariant(), name, rate, symbol, roundUnit, locked, active }!);
        return true;
    }

    public async Task<CurrencySyncResult> SyncCurrenciesAsync(
        Dictionary<string, decimal> rates, decimal vndRate, CancellationToken ct = default)
    {
        int countOk = 0;
        int countFail = 0;

        foreach (var rate in rates)
        {
            try
            {
                var currCode = rate.Key.ToUpperInvariant();
                var rateValue = rate.Value;
                await db.Database.ExecuteSqlAsync($"""
                    INSERT INTO currencies (code, rate, active)
                    VALUES ({currCode}, {rateValue}, true)
                    ON CONFLICT (code) DO UPDATE SET rate = EXCLUDED.rate
                    WHERE currencies.locked = false
                    """, ct);
                countOk++;
            }
            catch
            {
                countFail++;
            }
        }

        if (vndRate > 0)
        {
            await db.Database.ExecuteSqlAsync($"""
                INSERT INTO currencies (code, rate, active)
                VALUES ('VND IATA', {vndRate}, true)
                ON CONFLICT (code) DO UPDATE SET rate = EXCLUDED.rate
                WHERE currencies.locked = false
                """, ct);
        }

        return new CurrencySyncResult
        {
            Success = true,
            Message = $"Synced. OK: {countOk}, Failed: {countFail}",
            CountOk = countOk,
            CountFail = countFail
        };
    }
}
