using EventHub.Domain.Abstractions;
using EventHub.Domain.Orders;

namespace EventHub.Domain.Payments;

public sealed record PaymentCapturedEvent(
    PaymentId PaymentId,
    OrderId OrderId,
    string ProviderReference,
    DateTimeOffset OccurredOn) : DomainEvent(OccurredOn);
