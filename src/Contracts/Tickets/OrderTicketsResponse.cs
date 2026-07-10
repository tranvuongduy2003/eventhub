namespace EventHub.Contracts.Tickets;

public sealed record OrderTicketsResponse(
    int OrderId,
    string OrderStatus,
    List<TicketResponse> Tickets);
