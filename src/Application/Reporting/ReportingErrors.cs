using EventHub.Application.Common;

namespace EventHub.Application.Reporting;

public static class ReportingErrors
{
    public static Error InsufficientPermissions =>
        Error.Forbidden(
            "INSUFFICIENT_PERMISSIONS",
            "You do not have the required permissions to perform this operation on this event.");

    public static Error MessageSubjectRequired =>
        Error.Validation("MESSAGE_SUBJECT_REQUIRED", "Message subject is required.");

    public static Error MessageBodyRequired =>
        Error.Validation("MESSAGE_BODY_REQUIRED", "Message body is required.");

    public static Error AttendeesRequired =>
        Error.Validation("ATTENDEES_REQUIRED", "There are no attendees to message.");

    public static Error InvalidReminderLeadTime =>
        Error.Validation("INVALID_REMINDER_LEAD_TIME", "Reminder lead time must be greater than zero.");

    public static Error ReminderWindowAlreadyPassed =>
        Error.Validation("REMINDER_WINDOW_ALREADY_PASSED", "Reminder lead time must schedule before the event starts.");
}
