namespace EventHub.Domain.Abstractions;

public interface IAggregateRoot<out TId> : IEntity<TId>
    where TId : notnull
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}
