using MediatR;

namespace Shared.BuildingBlocks.Abstractions;

public interface IDomainEvent : INotification
{
    Guid EventId => Guid.NewGuid();
    DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
    string EventType => GetType()?.AssemblyQualifiedName ?? string.Empty;
}
