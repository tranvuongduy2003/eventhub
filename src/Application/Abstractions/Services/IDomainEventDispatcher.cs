using EventHub.Domain.Abstractions;

namespace EventHub.Application.Abstractions.Services;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
