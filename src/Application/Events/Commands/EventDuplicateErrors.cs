using EventHub.Application.Common;

namespace EventHub.Application.Events.Commands;

public static class EventDuplicateErrors
{
    public static readonly Error SourceEventNotFound = Error.NotFound(
        "EVENT_NOT_FOUND",
        "The event was not found.");
}
