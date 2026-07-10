using EventHub.Domain.Abstractions;
using EventHub.Domain.Events;
using EventHub.Domain.Orders;

namespace EventHub.Domain.Tickets;

public sealed record TicketIssuedEvent(
    TicketId TicketId,
    EventId EventId,
    OrderId OrderId,
    TicketCode Code,
    DateTimeOffset OccurredOn) : DomainEvent(OccurredOn);
