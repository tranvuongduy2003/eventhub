using EventHub.Domain.Abstractions;
using EventHub.Domain.Orders;

namespace EventHub.Domain.Payments;

public sealed record PaymentRefundedEvent(
    PaymentId PaymentId,
    OrderId OrderId,
    string ProviderReference,
    DateTimeOffset RefundedAt) : DomainEvent;
