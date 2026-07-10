namespace EventHub.Contracts.Reporting;

public sealed record EventReminderSettingsRequest(bool Enabled, int LeadTimeMinutes);

public sealed record EventReminderSettingsResponse(
    int EventId,
    bool Enabled,
    int LeadTimeMinutes,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? LastSentAt);
