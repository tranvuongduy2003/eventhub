using EventHub.Domain.Abstractions;
using EventHub.Domain.Exceptions;
using EventHub.Domain.Users;

namespace EventHub.Domain.Events;

public sealed class Event : AggregateRoot<EventId>
{
    private readonly List<Occurrence> _occurrences = [];

    private Event()
    {
    }

    public UserId OrganizerId { get; private set; }

    public IReadOnlyCollection<Occurrence> Occurrences => _occurrences.AsReadOnly();

    public EventTitle Title { get; private set; } = null!;

    public EventSchedule? Schedule { get; private set; }

    public EventLocation Location { get; private set; } = null!;

    public string? Description { get; private set; }

    public EventStatus Status { get; private set; }

    public Slug? Slug { get; private set; }

    public CoverImageRef? CoverImageRef { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? CancelledAt { get; private set; }

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

    public Event Duplicate(UserId organizerId, DateTimeOffset createdAt)
    {
        return new Event
        {
            OrganizerId = organizerId,
            Title = EventTitle.Create($"Copy of {Title.Value}"),
            Schedule = null,
            Location = Location,
            Description = Description,
            Status = EventStatus.Draft,
            Slug = null,
            CoverImageRef = CoverImageRef,
            CancelledAt = null,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            RowVersion = 1,
        };
    }

    public void SetCoverImage(CoverImageRef coverImageRef)
    {
        CoverImageRef = coverImageRef;
    }

    public void Publish(Slug slug, DateTimeOffset publishedAt)
    {
        if (Status is not EventStatus.Draft)
        {
            throw new BusinessRuleValidationException(
                "EVENT_NOT_PUBLISHABLE",
                Status switch
                {
                    EventStatus.Published => "The event is already published.",
                    EventStatus.Closed => "Cannot publish a closed event.",
                    EventStatus.Cancelled => "Cannot publish a cancelled event.",
                    _ => "The event cannot be published in its current status.",
                });
        }

        Status = EventStatus.Published;
        Slug = slug;
        UpdatedAt = publishedAt;

        Raise(new EventPublishedEvent(Id, slug));
    }

    public void Close(DateTimeOffset closedAt)
    {
        if (Status is not EventStatus.Published)
        {
            throw new BusinessRuleValidationException(
                "EVENT_NOT_CLOSABLE",
                Status switch
                {
                    EventStatus.Draft => "Cannot close a draft event.",
                    EventStatus.Closed => "The event is already closed.",
                    EventStatus.Cancelled => "Cannot close a cancelled event.",
                    _ => "The event cannot be closed in its current status.",
                });
        }

        Status = EventStatus.Closed;
        UpdatedAt = closedAt;

        Raise(new EventClosedEvent(Id, closedAt));
    }

    public void Cancel(DateTimeOffset cancelledAt)
    {
        if (Status is EventStatus.Draft or EventStatus.Cancelled)
        {
            throw new BusinessRuleValidationException(
                "EVENT_NOT_CANCELLABLE",
                Status switch
                {
                    EventStatus.Draft => "Cannot cancel a draft event.",
                    EventStatus.Cancelled => "The event is already cancelled.",
                    _ => "The event cannot be cancelled in its current status.",
                });
        }

        Status = EventStatus.Cancelled;
        CancelledAt = cancelledAt;
        UpdatedAt = cancelledAt;

        Raise(new EventCancelledEvent(Id, cancelledAt));
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

    public Occurrence ScheduleOccurrence(
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        string? venueName,
        string? address,
        DateTimeOffset createdAt)
    {
        if (Status is EventStatus.Closed or EventStatus.Cancelled)
        {
            throw new BusinessRuleValidationException(
                "EVENT_CLOSED_OR_CANCELLED",
                "Cannot add occurrences to a closed or cancelled event.");
        }

        var overlapping = _occurrences.Any(o =>
            startsAt < o.EndsAt && endsAt > o.StartsAt);

        if (overlapping)
        {
            throw new BusinessRuleValidationException(
                "OCCURRENCE_OVERLAPS",
                "The occurrence overlaps with an existing occurrence.");
        }

        var occurrence = Occurrence.Schedule(startsAt, endsAt, venueName, address, createdAt);

        _occurrences.Add(occurrence);

        UpdatedAt = createdAt;

        Raise(new OccurrenceScheduledEvent(Id, occurrence.Id, startsAt, endsAt));

        return occurrence;
    }

    public void RescheduleOccurrence(
        OccurrenceId occurrenceId,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        string? venueName,
        string? address,
        DateTimeOffset updatedAt)
    {
        if (Status is EventStatus.Closed or EventStatus.Cancelled)
        {
            throw new BusinessRuleValidationException(
                "EVENT_CLOSED_OR_CANCELLED",
                "Cannot edit occurrences on a closed or cancelled event.");
        }

        var occurrence = _occurrences.FirstOrDefault(o => o.Id == occurrenceId)
            ?? throw new BusinessRuleValidationException(
                "OCCURRENCE_NOT_FOUND",
                "The occurrence was not found.");

        var overlapping = _occurrences.Any(o =>
            o.Id != occurrenceId &&
            startsAt < o.EndsAt && endsAt > o.StartsAt);

        if (overlapping)
        {
            throw new BusinessRuleValidationException(
                "OCCURRENCE_OVERLAPS",
                "The updated occurrence would overlap with an existing occurrence.");
        }

        occurrence.Reschedule(startsAt, endsAt, venueName, address, updatedAt);

        UpdatedAt = updatedAt;

        Raise(new OccurrenceUpdatedEvent(Id, occurrenceId, startsAt, endsAt));
    }

    public void RemoveOccurrence(OccurrenceId occurrenceId, DateTimeOffset updatedAt)
    {
        if (Status is EventStatus.Closed or EventStatus.Cancelled)
        {
            throw new BusinessRuleValidationException(
                "EVENT_CLOSED_OR_CANCELLED",
                "Cannot remove occurrences from a closed or cancelled event.");
        }

        var occurrence = _occurrences.FirstOrDefault(o => o.Id == occurrenceId)
            ?? throw new BusinessRuleValidationException(
                "OCCURRENCE_NOT_FOUND",
                "The occurrence was not found.");

        _occurrences.Remove(occurrence);

        UpdatedAt = updatedAt;

        Raise(new OccurrenceRemovedEvent(Id, occurrenceId));
    }

    public void LoadOccurrences(List<Occurrence> occurrences)
    {
        _occurrences.Clear();
        _occurrences.AddRange(occurrences);
    }

    public static Event FromPersistence(
        EventId id,
        UserId organizerId,
        EventTitle title,
        EventSchedule? schedule,
        EventLocation location,
        string? description,
        EventStatus status,
        Slug? slug,
        CoverImageRef? coverImageRef,
        DateTimeOffset? cancelledAt,
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
            Slug = slug,
            CoverImageRef = coverImageRef,
            CancelledAt = cancelledAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            RowVersion = rowVersion,
        };
}
