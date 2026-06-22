using EventHub.Domain.Events;

namespace EventHub.Infrastructure.Persistence.Entities;

public sealed class EventRecord
{
    public int Id { get; set; }

    public Guid OrganizerId { get; set; }

    public required string Title { get; set; }

    public DateTimeOffset ScheduleStartsAt { get; set; }

    public DateTimeOffset ScheduleEndsAt { get; set; }

    public required string ScheduleTimeZoneId { get; set; }

    public string? LocationPhysicalAddress { get; set; }

    public bool LocationIsOnline { get; set; }

    public string? Description { get; set; }

    public EventStatus Status { get; set; }

    public string? Slug { get; set; }

    public string? CoverImageKey { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public long RowVersion { get; set; }
}
