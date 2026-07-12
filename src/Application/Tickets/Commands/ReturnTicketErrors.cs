using EventHub.Application.Common;

namespace EventHub.Application.Tickets.Commands;

public static class ReturnTicketErrors
{
    public static readonly Error TicketNotFound = Error.NotFound(
        "TICKET_NOT_FOUND",
        "The ticket was not found.");

    public static readonly Error OrderNotFound = Error.NotFound(
        "ORDER_NOT_FOUND",
        "The order was not found.");

    public static readonly Error EventNotFound = Error.NotFound(
        "EVENT_NOT_FOUND",
        "The event was not found.");

    public static readonly Error EventNotSoldOut = Error.Validation(
        "EVENT_NOT_SOLD_OUT",
        "Tickets can only be returned to the pool while the ticket type is sold out.");

    public static readonly Error ReturnCutoffPassed = Error.Validation(
        "RETURN_CUTOFF_PASSED",
        "Tickets can only be returned before the event starts.");
}
