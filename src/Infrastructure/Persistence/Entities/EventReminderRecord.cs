namespace EventHub.Infrastructure.Persistence.Entities;

public sealed class EventReminderRecord
{
    public int EventId { get; set; }

    public bool Enabled { get; set; }

    public int LeadTimeMinutes { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? LastSentAt { get; set; }
}
