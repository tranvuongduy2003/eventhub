using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Domain.Events;

namespace EventHub.Application.Events.EventHandlers;

internal sealed class EventCreatedEventHandler(
    IEventUserRoleRepository eventUserRoleRepository,
    IClock clock)
    : IDomainEventHandler<EventCreatedEvent>
{
    public async Task Handle(EventCreatedEvent domainEvent, CancellationToken cancellationToken)
    {
        var ownerRole = EventUserRole.Create(
            domainEvent.EventId,
            domainEvent.OrganizerId,
            EventRole.Owner,
            clock.UtcNow);

        await eventUserRoleRepository.AddAsync(ownerRole, cancellationToken);
    }
}
