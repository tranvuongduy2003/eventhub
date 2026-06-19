using EventHub.Domain.Abstractions;
using EventHub.Domain.Users;

namespace EventHub.Domain.Events;

public sealed record EventCreatedEvent(EventId EventId, UserId OrganizerId) : DomainEvent;
