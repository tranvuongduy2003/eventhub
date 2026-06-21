using EventHub.Domain.Abstractions;
using EventHub.Domain.Users;

namespace EventHub.Domain.Events;

public sealed class Event : AggregateRoot<EventId>
{
    private Event()
    {
    }

    public UserId OrganizerId { get; private set; }

    public EventTitle Title { get; private set; } = null!;

    public EventSchedule Schedule { get; private set; } = null!;

    public EventLocation Location { get; private set; } = null!;

    public EventStatus Status { get; private set; }

    public CoverImageRef? CoverImageRef { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public long RowVersion { get; private set; }

    public static Event CreateDraft(
        UserId organizerId,
        EventTitle title,
        EventSchedule schedule,
        EventLocation location,
        DateTimeOffset createdAt)
    {
        var draftEvent = new Event
        {
            OrganizerId = organizerId,
            Title = title,
            Schedule = schedule,
            Location = location,
            Status = EventStatus.Draft,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            RowVersion = 1,
        };

        return draftEvent;
    }

    public void MarkAsPersisted()
    {
        Raise(new EventCreatedEvent(Id, OrganizerId));
    }

    public void SetCoverImage(CoverImageRef coverImageRef)
    {
        CoverImageRef = coverImageRef;
    }

    public static Event FromPersistence(
        EventId id,
        UserId organizerId,
        EventTitle title,
        EventSchedule schedule,
        EventLocation location,
        EventStatus status,
        CoverImageRef? coverImageRef,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        long rowVersion) =>
        new()
        {
            Id = id,
            OrganizerId = organizerId,
            Title = title,
            Schedule = schedule,
            Location = location,
            Status = status,
            CoverImageRef = coverImageRef,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            RowVersion = rowVersion,
        };
}
