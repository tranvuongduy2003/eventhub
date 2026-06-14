using Solution.Domain.Abstractions;

namespace Solution.Application.Abstractions.Services;

public interface IPendingDomainEventsCollector
{
    void AddRange(IEnumerable<IDomainEvent> domainEvents);

    IReadOnlyCollection<IDomainEvent> Drain();
}
