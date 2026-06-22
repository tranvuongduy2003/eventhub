using EventHub.Domain.Abstractions;
using EventHub.Domain.Exceptions;
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

    public string? Description { get; private set; }

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
            Description = null,
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

    public void UpdateDetails(
        EventTitle title,
        EventSchedule schedule,
        EventLocation location,
        string? description,
        DateTimeOffset updatedAt)
    {
        if (Status is EventStatus.Closed or EventStatus.Cancelled)
        {
            throw new BusinessRuleValidationException(
                "EVENT_CLOSED_OR_CANCELLED",
                "Cannot edit a closed or cancelled event.");
        }

        Title = title;
        Schedule = schedule;
        Location = location;
        Description = description;
        UpdatedAt = updatedAt;
    }

    public static Event FromPersistence(
        EventId id,
        UserId organizerId,
        EventTitle title,
        EventSchedule schedule,
        EventLocation location,
        string? description,
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
            Description = description,
            Status = status,
            CoverImageRef = coverImageRef,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            RowVersion = rowVersion,
        };
}
