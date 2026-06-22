using EventHub.Domain.Events;
using EventHub.Domain.Exceptions;
using EventHub.Domain.Users;
using FluentAssertions;

namespace EventHub.Domain.UnitTests.Events;

public sealed class EventUpdateDetailsTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset StartsAt = new(2026, 7, 15, 14, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset EndsAt = new(2026, 7, 15, 16, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset UpdatedAt = new(2026, 7, 10, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void UpdateDetails_OnDraft_UpdatesAllFields()
    {
        var draftEvent = CreateDraftEvent();
        var newTitle = EventTitle.Create("Updated Conference 2026");
        var newSchedule = EventSchedule.Create(
            new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 1, 18, 0, 0, TimeSpan.Zero),
            "UTC");
        var newLocation = EventLocation.Create("456 New Ave", false);

        draftEvent.UpdateDetails(newTitle, newSchedule, newLocation, "New description", UpdatedAt);

        draftEvent.Title.Should().Be(newTitle);
        draftEvent.Schedule.Should().Be(newSchedule);
        draftEvent.Location.Should().Be(newLocation);
        draftEvent.Description.Should().Be("New description");
    }

    [Fact]
    public void UpdateDetails_OnPublished_UpdatesDescriptiveFields()
    {
        var publishedEvent = CreatePublishedEvent();
        var newTitle = EventTitle.Create("Updated Published Event");
        var newSchedule = EventSchedule.Create(StartsAt, EndsAt, "UTC");
        var newLocation = EventLocation.Create("789 Published Ave", false);

        publishedEvent.UpdateDetails(newTitle, newSchedule, newLocation, "Updated description", UpdatedAt);

        publishedEvent.Title.Should().Be(newTitle);
        publishedEvent.Description.Should().Be("Updated description");
        publishedEvent.Status.Should().Be(EventStatus.Published);
    }

    [Fact]
    public void UpdateDetails_OnClosed_ThrowsDomainException()
    {
        var closedEvent = CreateClosedEvent();
        var newTitle = EventTitle.Create("Should Not Update");
        var newSchedule = EventSchedule.Create(StartsAt, EndsAt, "UTC");
        var newLocation = EventLocation.Create("123 St", false);

        var act = () => closedEvent.UpdateDetails(newTitle, newSchedule, newLocation, null, UpdatedAt);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("EVENT_CLOSED_OR_CANCELLED");
    }

    [Fact]
    public void UpdateDetails_OnCancelled_ThrowsDomainException()
    {
        var cancelledEvent = CreateCancelledEvent();
        var newTitle = EventTitle.Create("Should Not Update");
        var newSchedule = EventSchedule.Create(StartsAt, EndsAt, "UTC");
        var newLocation = EventLocation.Create("123 St", false);

        var act = () => cancelledEvent.UpdateDetails(newTitle, newSchedule, newLocation, null, UpdatedAt);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("EVENT_CLOSED_OR_CANCELLED");
    }

    [Fact]
    public void UpdateDetails_DoesNotChangeStatusOrCoverImage()
    {
        var draftEvent = CreateDraftEvent();
        var originalStatus = draftEvent.Status;
        var originalCoverImage = draftEvent.CoverImageRef;

        draftEvent.UpdateDetails(
            EventTitle.Create("New Title"),
            EventSchedule.Create(StartsAt, EndsAt, "UTC"),
            EventLocation.Create("123 St", false),
            null,
            UpdatedAt);

        draftEvent.Status.Should().Be(originalStatus);
        draftEvent.CoverImageRef.Should().Be(originalCoverImage);
    }

    [Fact]
    public void UpdateDetails_SetsUpdatedAt()
    {
        var draftEvent = CreateDraftEvent();

        draftEvent.UpdateDetails(
            EventTitle.Create("New Title"),
            EventSchedule.Create(StartsAt, EndsAt, "UTC"),
            EventLocation.Create("123 St", false),
            null,
            UpdatedAt);

        draftEvent.UpdatedAt.Should().Be(UpdatedAt);
    }

    [Fact]
    public void UpdateDetails_WithNullDescription_SetsDescriptionToNull()
    {
        var draftEvent = CreateDraftEvent();

        draftEvent.UpdateDetails(
            EventTitle.Create("New Title"),
            EventSchedule.Create(StartsAt, EndsAt, "UTC"),
            EventLocation.Create("123 St", false),
            null,
            UpdatedAt);

        draftEvent.Description.Should().BeNull();
    }

    private static Event CreateDraftEvent() =>
        Event.CreateDraft(
            UserId.New(),
            EventTitle.Create("Tech Conference 2026"),
            EventSchedule.Create(StartsAt, EndsAt, "UTC"),
            EventLocation.Create("123 Conference Ave", false),
            CreatedAt);

    private static Event CreatePublishedEvent()
    {
        var publishedEvent = Event.FromPersistence(
            EventId.From(1),
            UserId.New(),
            EventTitle.Create("Published Event"),
            EventSchedule.Create(StartsAt, EndsAt, "UTC"),
            EventLocation.Create("123 Main St", false),
            null,
            EventStatus.Published,
            null,
            CreatedAt,
            CreatedAt,
            1);
        return publishedEvent;
    }

    private static Event CreateClosedEvent()
    {
        var closedEvent = Event.FromPersistence(
            EventId.From(2),
            UserId.New(),
            EventTitle.Create("Closed Event"),
            EventSchedule.Create(StartsAt, EndsAt, "UTC"),
            EventLocation.Create("123 Main St", false),
            null,
            EventStatus.Closed,
            null,
            CreatedAt,
            CreatedAt,
            1);
        return closedEvent;
    }

    private static Event CreateCancelledEvent()
    {
        var cancelledEvent = Event.FromPersistence(
            EventId.From(3),
            UserId.New(),
            EventTitle.Create("Cancelled Event"),
            EventSchedule.Create(StartsAt, EndsAt, "UTC"),
            EventLocation.Create("123 Main St", false),
            null,
            EventStatus.Cancelled,
            null,
            CreatedAt,
            CreatedAt,
            1);
        return cancelledEvent;
    }
}
