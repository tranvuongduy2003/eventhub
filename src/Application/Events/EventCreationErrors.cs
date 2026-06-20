using EventHub.Application.Common;

namespace EventHub.Application.Events;

public static class EventCreationErrors
{
    public static readonly Error Unauthorized = Error.Unauthorized(
        "UNAUTHORIZED",
        "You must be logged in to create an event.");
}
