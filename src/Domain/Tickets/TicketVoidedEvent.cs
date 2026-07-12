using EventHub.Domain.Abstractions;
using EventHub.Domain.Events;
using EventHub.Domain.Orders;

namespace EventHub.Domain.Tickets;

public sealed record TicketVoidedEvent(
    TicketId TicketId,
    EventId EventId,
    OrderId OrderId,
    TicketCode Code,
    DateTimeOffset VoidedAt) : DomainEvent;
