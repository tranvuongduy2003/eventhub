using EventHub.Domain.Abstractions;

namespace EventHub.Domain.Events;

public sealed record TicketTypeRemovedEvent(
    EventId EventId,
    TicketTypeId TicketTypeId) : DomainEvent;
