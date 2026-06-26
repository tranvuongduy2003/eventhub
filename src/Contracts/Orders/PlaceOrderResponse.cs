namespace EventHub.Contracts.Orders;

public sealed record PlaceOrderResponse(
    int OrderId,
    string Status,
    decimal TotalAmount,
    string TotalCurrency,
    int? PaymentId,
    DateTimeOffset PlacedAt,
    DateTimeOffset? ConfirmedAt,
    List<OrderLineResponse> Lines,
    string? DiscountCode = null,
    decimal? DiscountAmount = null);
