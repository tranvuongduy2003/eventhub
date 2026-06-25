using EventHub.Domain.Abstractions;

namespace EventHub.Domain.Events;

public sealed record TicketTypeUpdatedEvent(
    EventId EventId,
    TicketTypeId TicketTypeId) : DomainEvent;
