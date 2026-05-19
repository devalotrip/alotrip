using System.Reflection;
using System.Text.Json;
using Flight.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.BuildingBlocks.Abstractions;

namespace Flight.Infrastructure.BackgroundJobs;

/// <summary>
/// Processes the Outbox — reads unprocessed OutboxMessages every 15 seconds,
/// deserializes domain events, publishes via MediatR, marks them processed.
///
/// Clean Architecture: Infrastructure concern only — Application layer raises events,
/// this job dispatches them to handlers (email, audit log, etc.) with reliable retry.
/// </summary>
public sealed class OutboxProcessorJob(
    IServiceScopeFactory          scopeFactory,
    ILogger<OutboxProcessorJob>   logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for application startup to complete
        await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessBatchAsync(stoppingToken);
            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db       = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var now      = DateTimeOffset.UtcNow;

            var messages = await db.OutboxMessages
                .Where(m => m.ProcessedOnUtc == null
                    && !m.IsPermanentFail
                    && (m.NextAttemptOnUtc == null || m.NextAttemptOnUtc <= now))
                .OrderBy(m => m.OccurredOn)
                .Take(50)
                .ToListAsync(ct);

            if (messages.Count == 0) return;

            foreach (var msg in messages)
            {
                try
                {
                    // Deserialize the domain event from JSON + assembly-qualified type name
                    var eventType = Type.GetType(msg.EventType);
                    if (eventType is null)
                    {
                        logger.LogError("[Outbox] Unknown event type '{Type}' for id={Id}.",
                            msg.EventType, msg.Id);
                        msg.RecordFailedAttempt($"Unknown type: {msg.EventType}", now);
                        continue;
                    }

                    var domainEvent = JsonSerializer.Deserialize(msg.Payload, eventType) as IDomainEvent;
                    if (domainEvent is null)
                    {
                        logger.LogError("[Outbox] Failed to deserialize event id={Id} of type '{Type}'.",
                            msg.Id, msg.EventType);
                        msg.RecordFailedAttempt($"Deserialization returned null for type {msg.EventType}", now);
                        continue;
                    }

                    // Dispatch to MediatR handlers
                    await mediator.Publish(domainEvent, ct);

                    msg.MarkProcessed(now);
                    logger.LogDebug("[Outbox] Processed event {Type} id={Id}", msg.EventType, msg.Id);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "[Outbox] Failed to process event id={Id}, attempt={Attempt}",
                        msg.Id, msg.AttemptCount + 1);
                    msg.RecordFailedAttempt(ex.Message, now);
                }
            }

            await db.SaveChangesAsync(ct);

            var processed = messages.Count(m => m.ProcessedOnUtc != null);
            var failed    = messages.Count - processed;
            if (processed > 0)
                logger.LogInformation("[Outbox] Processed {Processed} events, {Failed} failed/retrying.", processed, failed);
        }
        catch (OperationCanceledException) { /* shutdown */ }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Outbox] Batch processing failed.");
        }
    }
}
