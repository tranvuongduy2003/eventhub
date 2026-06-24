using EventHub.Domain.Abstractions;

namespace EventHub.Domain.Orders;

public sealed record OrderCancelledEvent(
    OrderId OrderId,
    DateTimeOffset OccurredOn) : DomainEvent(OccurredOn);
