using EventHub.Domain.Abstractions;
using EventHub.Domain.Events;
using EventHub.Domain.Orders;

namespace EventHub.Domain.Tickets;

public sealed record TicketTransferredEvent(
    TicketId SourceTicketId,
    TicketId ReplacementTicketId,
    EventId EventId,
    OrderId OrderId,
    TicketCode SourceCode,
    TicketCode ReplacementCode,
    DateTimeOffset OccurredOn) : DomainEvent(OccurredOn);
