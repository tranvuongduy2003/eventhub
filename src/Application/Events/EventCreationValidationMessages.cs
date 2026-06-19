namespace EventHub.Application.Events;

internal static class EventCreationValidationMessages
{
    public const string TitleRequired = "Event title is required.";

    public const string TitleTooLong = "Event title must be between 1 and 200 characters.";

    public const string EndsBeforeStart = "Event end time must be after start time.";

    public const string TimeZoneRequired = "Time zone is required.";

    public const string TimeZoneInvalid = "Time zone is not valid.";

    public const string LocationRequired = "Event must have a physical address or be marked as online.";
}
