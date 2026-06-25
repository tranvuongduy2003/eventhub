using EventHub.Application.Common;

namespace EventHub.Application.Events.Commands;

public static class TicketTypeErrors
{
    public static readonly Error EventNotFound = Error.NotFound(
        "EVENT_NOT_FOUND",
        "The event was not found.");

    public static readonly Error TicketTypeNotFound = Error.NotFound(
        "TICKET_TYPE_NOT_FOUND",
        "The ticket type was not found on this event.");

    public static readonly Error TicketTypeNameDuplicate = Error.Validation(
        "TICKET_TYPE_NAME_DUPLICATE",
        "A ticket type with this name already exists on the event.");

    public static readonly Error TicketTypeMaxReached = Error.Validation(
        "TICKET_TYPE_MAX_REACHED",
        "An event cannot have more than 10 ticket types.");

    public static readonly Error TicketTypeHasSales = Error.Validation(
        "TICKET_TYPE_HAS_SALES",
        "Cannot remove a ticket type that has reserved or sold tickets.");

    public static readonly Error TicketTypeLastOnPublishedEvent = Error.Validation(
        "TICKET_TYPE_LAST_ON_PUBLISHED_EVENT",
        "Cannot remove the last ticket type from a published event. Unpublish or cancel the event first.");
}
