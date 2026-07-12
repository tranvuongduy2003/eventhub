using EventHub.Application.Abstractions.Messaging;

namespace EventHub.Application.Tickets.Commands;

public sealed record ReturnTicketCommand(int OrderId, int TicketId) : ICommand<ReturnTicketResult>;

public sealed record ReturnTicketResult(
    int TicketId,
    int OrderId,
    int EventId,
    string TicketStatus,
    string OrderStatus,
    string? PaymentStatus);
