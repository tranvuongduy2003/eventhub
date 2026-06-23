namespace EventHub.Infrastructure.Persistence.Entities;

public sealed class OccurrenceRecord
{
    public int Id { get; set; }

    public int EventId { get; set; }

    public DateTimeOffset StartsAt { get; set; }

    public DateTimeOffset EndsAt { get; set; }

    public string? VenueName { get; set; }

    public string? Address { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
