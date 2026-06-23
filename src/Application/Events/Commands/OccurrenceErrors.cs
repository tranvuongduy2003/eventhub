using EventHub.Application.Common;

namespace EventHub.Application.Events.Commands;

public static class OccurrenceErrors
{
    public static readonly Error EventNotFound = Error.NotFound(
        "EVENT_NOT_FOUND",
        "The event was not found.");

    public static readonly Error OccurrenceNotFound = Error.NotFound(
        "OCCURRENCE_NOT_FOUND",
        "The occurrence was not found.");
}
