using EventHub.Domain.Abstractions;

namespace EventHub.Application.Abstractions.Services;

public interface IPendingDomainEventsCollector
{
    void AddRange(IEnumerable<IDomainEvent> domainEvents);

    IReadOnlyCollection<IDomainEvent> Drain();
}
