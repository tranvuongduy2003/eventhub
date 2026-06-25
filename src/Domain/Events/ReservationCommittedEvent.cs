using EventHub.Domain.Abstractions;

namespace EventHub.Domain.Events;

public sealed record ReservationCommittedEvent(
    EventId EventId,
    TicketTypeId TicketTypeId,
    ReservationId ReservationId,
    int Quantity,
    DateTimeOffset OccurredOn) : DomainEvent(OccurredOn);
