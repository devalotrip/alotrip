using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shared.BuildingBlocks.Abstractions;

namespace Flight.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Tự động set CreatedOnUtc và LastModifiedOnUtc khi SaveChanges.
/// Supports both Entity&lt;Guid&gt; and Entity&lt;int&gt;.
/// </summary>
public sealed class AuditableEntityInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateTimestamps(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateTimestamps(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void UpdateTimestamps(DbContext? context)
    {
        if (context is null) return;

        var now = DateTime.UtcNow;

        // Handle Entity<Guid> (bookings, passengers, flights, segments, tickets)
        foreach (var entry in context.ChangeTracker.Entries<Entity<Guid>>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedOnUtc = now;

            if (entry.State is EntityState.Added or EntityState.Modified)
                entry.Entity.LastModifiedOnUtc = now;
        }

        // Handle Entity<int> (agents, airlines, aircrafts, baggages, search analytics, etc.)
        foreach (var entry in context.ChangeTracker.Entries<Entity<int>>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedOnUtc = now;

            if (entry.State is EntityState.Added or EntityState.Modified)
                entry.Entity.LastModifiedOnUtc = now;
        }
    }
}
