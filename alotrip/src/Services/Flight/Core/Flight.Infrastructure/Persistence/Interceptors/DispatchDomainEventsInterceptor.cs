using Flight.Domain.Aggregates.Outbox;
using Flight.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Shared.BuildingBlocks.Abstractions;

namespace Flight.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Outbox-aware domain event dispatcher:
/// 1. Collects domain events from tracked aggregates.
/// 2. Persists them as OutboxMessages in the SAME transaction (atomic with domain change).
/// 3. After SaveChanges succeeds, publishes events in-process via MediatR (best-effort).
/// 4. Marks outbox messages as processed on success. Failures are retried by OutboxProcessorJob.
/// </summary>
public sealed class DispatchDomainEventsInterceptor(
    IMediator mediator,
    ILogger<DispatchDomainEventsInterceptor> logger)
    : SaveChangesInterceptor
{
    // Store events collected during SavingChanges for post-save dispatch
    [ThreadStatic] private static List<(OutboxMessageEntity Outbox, IDomainEvent Event)>? _pendingEvents;

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        CollectAndPersistEvents(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        CollectAndPersistEvents(eventData.Context);
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        DispatchAndMarkProcessed(eventData.Context).GetAwaiter().GetResult();
        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result,
        CancellationToken cancellationToken = default)
    {
        await DispatchAndMarkProcessed(eventData.Context);
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// Pre-save: collect domain events, create outbox rows, add to context.
    /// </summary>
    private void CollectAndPersistEvents(DbContext? context)
    {
        if (context is null) return;

        var aggregates = context.ChangeTracker
            .Entries<IAggregate>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = aggregates.SelectMany(a => a.DomainEvents).ToList();
        aggregates.ForEach(a => a.ClearDomainEvents());

        if (domainEvents.Count == 0)
        {
            _pendingEvents = null;
            return;
        }

        var pending = new List<(OutboxMessageEntity, IDomainEvent)>();
        foreach (var evt in domainEvents)
        {
            var outbox = OutboxMessageEntity.Create(evt);
            context.Set<OutboxMessageEntity>().Add(outbox);
            pending.Add((outbox, evt));
        }

        _pendingEvents = pending;
    }

    /// <summary>
    /// Post-save: dispatch events in-process via MediatR.
    /// On success, mark outbox message as processed. On failure, leave for OutboxProcessorJob.
    /// </summary>
    private async Task DispatchAndMarkProcessed(DbContext? context)
    {
        var pending = _pendingEvents;
        _pendingEvents = null;

        if (pending is null || pending.Count == 0 || context is null) return;

        var now = DateTimeOffset.UtcNow;
        bool anyProcessed = false;

        foreach (var (outbox, evt) in pending)
        {
            try
            {
                await mediator.Publish(evt);
                outbox.MarkProcessed(now);
                anyProcessed = true;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "[Outbox] In-process dispatch failed for {EventType} id={Id}. Will retry via background job.",
                    outbox.EventType, outbox.Id);
                // Leave outbox message unprocessed — OutboxProcessorJob will pick it up
            }
        }

        if (anyProcessed)
        {
            // Save the ProcessedOnUtc updates in a separate save
            // (this is best-effort; if it fails, the outbox job will re-dispatch but handlers should be idempotent)
            try
            {
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[Outbox] Failed to persist ProcessedOnUtc marks. Events may be re-dispatched.");
            }
        }
    }
}
