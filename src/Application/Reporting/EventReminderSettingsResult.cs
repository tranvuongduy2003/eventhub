namespace EventHub.Application.Reporting;

public sealed record EventReminderSettingsResult(
    int EventId,
    bool Enabled,
    int LeadTimeMinutes,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? LastSentAt);

public sealed record DueEventReminderResult(
    int EventId,
    string EventTitle,
    DateTimeOffset StartsAt,
    string? TimeZoneId,
    int LeadTimeMinutes);
