using EventHub.Application.Abstractions.Messaging;

namespace EventHub.Application.Orders.Commands;

public sealed record PlaceOrderCommand(
    int EventId,
    string ContactName,
    string ContactEmail,
    List<PlaceOrderLineRequest> Lines,
    string? DiscountCode = null)
    : ICommand<PlaceOrderResult>;

public sealed record PlaceOrderLineRequest(
    int TicketTypeId,
    int Quantity);

public sealed record PlaceOrderResult(
    int OrderId,
    string Status,
    decimal TotalAmount,
    string TotalCurrency,
    int? PaymentId,
    DateTimeOffset PlacedAt,
    DateTimeOffset? ConfirmedAt,
    List<PlaceOrderLineResult> Lines,
    string? DiscountCode = null,
    decimal? DiscountAmount = null);

public sealed record PlaceOrderLineResult(
    int OrderLineId,
    int TicketTypeId,
    int Quantity,
    decimal UnitPriceAmount,
    string UnitPriceCurrency,
    decimal LineTotalAmount,
    string LineTotalCurrency);
