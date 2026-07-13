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

    public static readonly Error ReplayPayloadMismatch = Error.Validation(
        "CHECK_IN_REPLAY_PAYLOAD_MISMATCH",
        "This scan identifier was already used with different ticket data.");

    public static readonly Error TicketAlreadyCheckedIn = Error.Validation(
        "TICKET_ALREADY_CHECKED_IN",
        "This ticket has already been checked in.");

    public static readonly Error TicketWrongEvent = Error.Validation(
        "TICKET_WRONG_EVENT",
        "This ticket is for a different event.");

    public static readonly Error TicketNotValidForCheckIn = Error.Validation(
        "TICKET_NOT_VALID_FOR_CHECK_IN",
        "This ticket is not valid for check-in.");

    public static readonly Error TicketCannotBeCheckedIn = Error.Validation(
        "TICKET_CANNOT_BE_CHECKED_IN",
        "This ticket cannot be checked in.");

    public static readonly Error SearchTermRequired = Error.Validation(
        "CHECK_IN_SEARCH_REQUIRED",
        "Enter a ticket code or buyer email to search.");

    public static string TicketAlreadyCheckedInAt(DateTimeOffset checkedInAt) =>
        $"This ticket was already checked in at {checkedInAt:O}.";

    public static string ToStableReplayRejectionReason(
        Error error,
        DateTimeOffset? checkedInAt) =>
        error.Code switch
        {
            "TICKET_NOT_FOUND" => UnknownTicket.Message,
            "TICKET_ORDER_NOT_CONFIRMED" => TicketOrderNotConfirmed.Message,
            "TICKET_ALREADY_CHECKED_IN" when checkedInAt is { } firstCheckInAt =>
                TicketAlreadyCheckedInAt(firstCheckInAt),
            "TICKET_ALREADY_CHECKED_IN" => TicketAlreadyCheckedIn.Message,
            "TICKET_WRONG_EVENT" => TicketWrongEvent.Message,
            "TICKET_NOT_VALID_FOR_CHECK_IN" => TicketNotValidForCheckIn.Message,
            _ => TicketCannotBeCheckedIn.Message
        };
}
