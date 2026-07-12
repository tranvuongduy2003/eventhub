using EventHub.Domain.Abstractions;

namespace EventHub.Domain.Orders;

public sealed record OrderRefundedEvent(OrderId OrderId, DateTimeOffset RefundedAt) : DomainEvent;
