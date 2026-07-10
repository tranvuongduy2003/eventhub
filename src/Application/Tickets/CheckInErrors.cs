using EventHub.Application.Common;

namespace EventHub.Application.Tickets;

public static class CheckInErrors
{
    public static readonly Error UnknownTicket = Error.NotFound(
        "TICKET_NOT_FOUND",
        "No ticket was found for that code.");

    public static readonly Error TicketNotFound = Error.NotFound(
        "TICKET_NOT_FOUND",
        "The ticket was not found for this event.");

    public static readonly Error TicketOrderNotConfirmed = Error.Validation(
        "TICKET_ORDER_NOT_CONFIRMED",
        "This ticket's order is cancelled or not confirmed.");

    public static readonly Error SearchTermRequired = Error.Validation(
        "CHECK_IN_SEARCH_REQUIRED",
        "Enter a ticket code or buyer email to search.");
}
