using EventHub.Domain.Abstractions;

namespace EventHub.Domain.Events;

public sealed record InventoryReturnedToPoolEvent(
    EventId EventId,
    TicketTypeId TicketTypeId,
    int Quantity,
    DateTimeOffset OccurredOn) : DomainEvent(OccurredOn);
