using EventHub.Domain.Abstractions;
using EventHub.Domain.Events;
using EventHub.Domain.Orders;

namespace EventHub.Domain.Tickets;

public sealed record TicketCheckedInEvent(
    TicketId TicketId,
    EventId EventId,
    OrderId OrderId,
    TicketCode Code,
    DateTimeOffset CheckedInAt,
    DateTimeOffset OccurredOn) : DomainEvent(OccurredOn);
