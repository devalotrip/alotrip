using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Flight.Infrastructure.Persistence;

namespace Flight.Infrastructure.BackgroundJobs;

/// <summary>
/// Deletes expired / revoked refresh tokens every hour to keep the table lean.
/// Migrated logic: legacy SOAP services never cleaned up tokens;
/// this prevents the refresh_tokens table from growing unboundedly.
/// </summary>
public sealed class RefreshTokenCleanupJob(
    IServiceScopeFactory              scopeFactory,
    ILogger<RefreshTokenCleanupJob>   logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await CleanupAsync(stoppingToken);
            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task CleanupAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Delete tokens that have expired or been revoked more than 7 days ago
            int deleted = await db.Database.ExecuteSqlRawAsync("""
                DELETE FROM refresh_tokens
                WHERE  expires_at < NOW() - INTERVAL '7 days'
                   OR  revoked_at < NOW() - INTERVAL '7 days'
                """, ct);

            if (deleted > 0)
                logger.LogInformation("[TokenCleanup] Deleted {Count} stale refresh tokens.", deleted);
        }
        catch (OperationCanceledException) { /* shutdown */ }
        catch (Exception ex)
        {
            logger.LogError(ex, "[TokenCleanup] Cleanup failed.");
        }
    }
}
