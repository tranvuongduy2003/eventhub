namespace EventHub.Contracts.Tickets;

public sealed record ReturnTicketResponse(
    int TicketId,
    int OrderId,
    int EventId,
    string TicketStatus,
    string OrderStatus,
    string? PaymentStatus);
