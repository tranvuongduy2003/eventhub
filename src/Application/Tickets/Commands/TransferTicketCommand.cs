using EventHub.Application.Abstractions.Messaging;

namespace EventHub.Application.Tickets.Commands;

public sealed record TransferTicketCommand(
    int OrderId,
    int TicketId,
    string RecipientName,
    string RecipientEmail) : ICommand<TicketResult>;
