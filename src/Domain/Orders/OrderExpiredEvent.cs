using EventHub.Domain.Abstractions;

namespace EventHub.Domain.Orders;

public sealed record OrderExpiredEvent(
    OrderId OrderId,
    DateTimeOffset OccurredOn) : DomainEvent(OccurredOn);
