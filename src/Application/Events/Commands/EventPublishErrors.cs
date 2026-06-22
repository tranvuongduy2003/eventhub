using EventHub.Application.Common;

namespace EventHub.Application.Events.Commands;

public static class EventPublishErrors
{
    public static readonly Error EventNotFound = Error.NotFound(
        "EVENT_NOT_FOUND",
        "The event was not found.");

    public static readonly Error EventNotPublishable = Error.Validation(
        "EVENT_NOT_PUBLISHABLE",
        "The event cannot be published in its current status.");
}
