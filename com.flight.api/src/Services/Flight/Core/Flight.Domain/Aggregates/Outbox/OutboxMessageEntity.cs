using Shared.BuildingBlocks.Abstractions;
using System.Text.Json;

namespace Flight.Domain.Aggregates.Outbox;

/// <summary>
/// Lưu domain events vào DB cùng transaction để publish sau (Outbox Pattern).
/// </summary>
public sealed class OutboxMessageEntity : Entity<Guid>
{
    public const int MaxAttempts = 5;

    public string         EventType        { get; private set; } = default!;
    public string         Payload          { get; private set; } = default!;
    public DateTimeOffset OccurredOn       { get; private set; }
    public DateTimeOffset? ProcessedOnUtc  { get; private set; }
    public string?         ErrorMessage    { get; private set; }
    public int             AttemptCount    { get; private set; }
    public DateTimeOffset? NextAttemptOnUtc { get; private set; }
    public bool            IsPermanentFail { get; private set; }
    public string?         ClaimId         { get; private set; }
    public DateTimeOffset? ClaimedUntil    { get; private set; }

    private OutboxMessageEntity() { }

    public static OutboxMessageEntity Create(IDomainEvent domainEvent)
    {
        return new OutboxMessageEntity
        {
            Id          = Guid.NewGuid(),
            EventType   = domainEvent.EventType,
            Payload     = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
            OccurredOn  = domainEvent.OccurredOn,
            CreatedOnUtc = DateTime.UtcNow
        };
    }

    public bool IsClaimed(DateTimeOffset now)
        => ClaimId != null && ClaimedUntil.HasValue && ClaimedUntil.Value > now;

    public bool TryClaim(string claimId, DateTimeOffset now, TimeSpan duration)
    {
        if (IsClaimed(now)) return false;
        ClaimId      = claimId;
        ClaimedUntil = now + duration;
        return true;
    }

    public void MarkProcessed(DateTimeOffset now)
    {
        ProcessedOnUtc = now;
        ClaimId        = null;
        ClaimedUntil   = null;
    }

    public void RecordFailedAttempt(string error, DateTimeOffset now)
    {
        AttemptCount++;
        ErrorMessage = error;
        ClaimId      = null;
        ClaimedUntil = null;

        if (AttemptCount >= MaxAttempts)
        {
            IsPermanentFail = true;
            ErrorMessage    = $"Max {MaxAttempts} attempts exceeded. Last: {error}";
        }
        else
        {
            var delay  = TimeSpan.FromSeconds(Math.Pow(2, AttemptCount - 1));
            var maxDelay = TimeSpan.FromMinutes(5);
            var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000));
            NextAttemptOnUtc = now + TimeSpan.FromTicks(Math.Min(delay.Ticks, maxDelay.Ticks)) + jitter;
        }
    }
}
