using EventHub.Application.Abstractions.Services;
using EventHub.Domain.Abstractions;

namespace EventHub.Application.Services;

internal sealed class PendingDomainEventsCollector : IPendingDomainEventsCollector
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public void AddRange(IEnumerable<IDomainEvent> domainEvents) =>
        _domainEvents.AddRange(domainEvents);

    public IReadOnlyCollection<IDomainEvent> Drain()
    {
        if (_domainEvents.Count == 0)
        {
            return [];
        }

        var drained = _domainEvents.ToArray();
        _domainEvents.Clear();
        return drained;
    }
}
