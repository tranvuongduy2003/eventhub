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

    public static readonly Error TicketTypeSoldOut = Error.Validation(
        "TICKET_TYPE_SOLD_OUT",
        "One or more ticket types are sold out.");

    public static readonly Error EventNotPublishedForReservation = Error.Validation(
        "EVENT_NOT_PUBLISHED",
        "Cannot reserve tickets for an event that is not published.");

    public static readonly Error ConcurrencyConflict = Error.Conflict(
        "ORDER_CONCURRENCY_CONFLICT",
        "A concurrency conflict occurred. Please try again.");
}
