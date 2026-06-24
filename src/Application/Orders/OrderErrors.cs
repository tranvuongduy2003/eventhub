using EventHub.Application.Common;

namespace EventHub.Application.Orders;

public static class OrderErrors
{
    public static readonly Error EventNotFound = Error.NotFound(
        "ORDER_EVENT_NOT_FOUND",
        "The event was not found.");

    public static readonly Error EventNotPublished = Error.Validation(
        "ORDER_EVENT_NOT_PUBLISHED",
        "The event is not published.");

    public static readonly Error TicketTypeNotFound = Error.NotFound(
        "ORDER_TICKET_TYPE_NOT_FOUND",
        "One or more ticket types were not found.");

    public static readonly Error InsufficientAvailability = Error.Validation(
        "ORDER_INSUFFICIENT_AVAILABILITY",
        "Insufficient ticket availability for one or more ticket types.");

    public static readonly Error NoItems = Error.Validation(
        "ORDER_NO_ITEMS",
        "An order must contain at least one line item.");
}
