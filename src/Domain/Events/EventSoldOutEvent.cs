using EventHub.Domain.Abstractions;

namespace EventHub.Domain.Events;

public sealed record EventSoldOutEvent(
    EventId EventId,
    TicketTypeId TicketTypeId,
    DateTimeOffset OccurredOn) : DomainEvent(OccurredOn);
