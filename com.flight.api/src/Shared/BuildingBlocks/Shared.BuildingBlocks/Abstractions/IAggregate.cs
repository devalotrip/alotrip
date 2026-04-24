namespace Shared.BuildingBlocks.Abstractions;

public interface IAggregate
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}

public interface IAggregate<TId> : IAggregate where TId : notnull
{
    TId Id { get; }
}
