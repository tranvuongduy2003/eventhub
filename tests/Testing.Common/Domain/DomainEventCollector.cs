using Solution.Domain.Abstractions;

namespace Solution.Testing.Common.Domain;

public sealed class DomainEventCollector
{
    private readonly List<IDomainEvent> _events = [];

    public IReadOnlyList<IDomainEvent> Events => _events;

    public void Record(IDomainEvent domainEvent) => _events.Add(domainEvent);

    public void Clear() => _events.Clear();
}
