using EventHub.Domain.Abstractions;
using EventHub.Domain.Orders;

namespace EventHub.Domain.Payments;

public sealed record PaymentInitiatedEvent(
    PaymentId PaymentId,
    OrderId OrderId,
    decimal Amount,
    string Currency,
    string ProviderReference,
    DateTimeOffset OccurredOn) : DomainEvent(OccurredOn);
