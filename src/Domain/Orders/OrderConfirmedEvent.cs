using EventHub.Domain.Abstractions;
using EventHub.Domain.Events;

namespace EventHub.Domain.Orders;

public sealed record OrderConfirmedEvent(
    OrderId OrderId,
    EventId EventId,
    DateTimeOffset OccurredOn) : DomainEvent(OccurredOn);
