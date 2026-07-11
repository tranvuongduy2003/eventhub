using EventHub.Application.Common;

namespace EventHub.Application.Tickets.Commands;

public static class TransferTicketErrors
{
    public static readonly Error TicketNotFound = Error.NotFound(
        "TICKET_NOT_FOUND",
        "The ticket was not found for this order.");

    public static readonly Error TicketOrderNotConfirmed = Error.Validation(
        "TICKET_ORDER_NOT_CONFIRMED",
        "This ticket's order is cancelled or not confirmed.");
}
