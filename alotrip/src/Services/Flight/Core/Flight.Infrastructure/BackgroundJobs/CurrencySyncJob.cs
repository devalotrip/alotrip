using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Flight.Infrastructure.Persistence;

namespace Flight.Infrastructure.BackgroundJobs;

/// <summary>
/// Syncs currency exchange rates from openexchangerates.org every 6 hours.
/// Migrated from Admin/Job/QuartzJob.cs (Quartz.NET → IHostedService).
/// Only updates currencies where locked = false (same rule as legacy UpdateRate).
/// </summary>
public sealed class CurrencySyncJob(
    IServiceScopeFactory   scopeFactory,
    IConfiguration         config,
    IHttpClientFactory     httpFactory,
    ILogger<CurrencySyncJob> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Delay a bit on startup to let EF migrations run first
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await SyncAsync(stoppingToken);
            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task SyncAsync(CancellationToken ct)
    {
        try
        {
            string appId = config["OpenExchangeRates:AppId"]
                ?? "5b3f001435bd4ceaabe0ece2892c730b";

            string url = $"https://openexchangerates.org/api/latest.json?app_id={appId}";

            using var client = httpFactory.CreateClient("OpenExchangeRates");
            using var resp   = await client.GetAsync(url, ct);
            resp.EnsureSuccessStatusCode();

            string json = await resp.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("rates", out var rates))
            {
                logger.LogWarning("[CurrencySync] No 'rates' in response.");
                return;
            }

            // Build dict: code → rate-vs-USD (openexchangerates base = USD)
            var rateMap = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in rates.EnumerateObject())
            {
                if (decimal.TryParse(prop.Value.GetRawText(),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out decimal val))
                {
                    rateMap[prop.Name] = val;
                }
            }

            // VND IATA mirrors VND
            if (rateMap.TryGetValue("VND", out decimal vndRate))
                rateMap["VND IATA"] = vndRate;

            // Update DB: only update unlocked currencies that exist in the table
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            int updated = 0;
            foreach (var (code, rate) in rateMap)
            {
                // raw SQL: update only where locked = false
                int rows = await db.Database.ExecuteSqlRawAsync(
                    "UPDATE currencies SET rate = {0} WHERE code = {1} AND locked = false",
                    rate, code, ct);
                updated += rows;
            }

            logger.LogInformation("[CurrencySync] Updated {Count} currencies.", updated);
        }
        catch (OperationCanceledException) { /* shutdown */ }
        catch (Exception ex)
        {
            logger.LogError(ex, "[CurrencySync] Sync failed.");
        }
    }
}
