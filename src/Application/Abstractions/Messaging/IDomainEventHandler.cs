using EventHub.Domain.Abstractions;

namespace EventHub.Application.Abstractions.Messaging;

public interface IDomainEventHandler<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    Task Handle(TDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
