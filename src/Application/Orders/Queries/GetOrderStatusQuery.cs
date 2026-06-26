using EventHub.Application.Abstractions.Messaging;

namespace EventHub.Application.Orders.Queries;

public sealed record GetOrderStatusQuery(int OrderId)
    : IQuery<GetOrderStatusResult>;

public sealed record GetOrderStatusResult(
    int OrderId,
    string Status,
    decimal TotalAmount,
    string TotalCurrency,
    int? PaymentId,
    DateTimeOffset PlacedAt,
    DateTimeOffset? ConfirmedAt,
    List<GetOrderStatusLineResult> Lines,
    string? DiscountCode = null,
    decimal? DiscountAmount = null);

public sealed record GetOrderStatusLineResult(
    int OrderLineId,
    int TicketTypeId,
    int Quantity,
    decimal UnitPriceAmount,
    string UnitPriceCurrency,
    decimal LineTotalAmount,
    string LineTotalCurrency);
