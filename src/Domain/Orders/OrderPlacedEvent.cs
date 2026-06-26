using EventHub.Domain.Abstractions;
using EventHub.Domain.DiscountCodes;
using EventHub.Domain.Events;

namespace EventHub.Domain.Orders;

public sealed record OrderPlacedEvent(
    OrderId OrderId,
    EventId EventId,
    decimal TotalAmount,
    string TotalCurrency,
    DiscountCodeId? DiscountCodeId,
    decimal? DiscountAmount,
    DateTimeOffset OccurredOn) : DomainEvent(OccurredOn);
