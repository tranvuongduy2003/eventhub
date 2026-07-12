using EventHub.Domain.Abstractions;
using EventHub.Domain.Events;
using EventHub.Domain.Orders;

namespace EventHub.Domain.Tickets;

public sealed record TicketReturnedEvent(
    TicketId TicketId,
    EventId EventId,
    OrderId OrderId,
    TicketTypeId TicketTypeId,
    TicketCode Code,
    DateTimeOffset ReturnedAt) : DomainEvent;
