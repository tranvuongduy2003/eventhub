namespace EventHub.Contracts.Orders;

public sealed record PlaceOrderRequest(
    string ContactName,
    string ContactEmail,
    List<PlaceOrderLineRequest> Lines);

public sealed record PlaceOrderLineRequest(
    int TicketTypeId,
    int Quantity);
