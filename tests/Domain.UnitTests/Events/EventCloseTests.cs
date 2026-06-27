using EventHub.Domain.Events;
using EventHub.Domain.Exceptions;
using EventHub.Domain.Users;
using FluentAssertions;

namespace EventHub.Domain.UnitTests.Events;

public sealed class EventCloseTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset StartsAt = new(2026, 7, 15, 14, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset EndsAt = new(2026, 7, 15, 16, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PublishedAt = new(2026, 7, 2, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ClosedAt = new(2026, 7, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Close_PublishedEvent_TransitionsToClosedStatus()
    {
        var publishedEvent = CreatePublishedEvent();

        publishedEvent.Close(ClosedAt);

        publishedEvent.Status.Should().Be(EventStatus.Closed);
    }

    [Fact]
    public void Close_PublishedEvent_UpdatesTimestamp()
    {
        var publishedEvent = CreatePublishedEvent();

        publishedEvent.Close(ClosedAt);

        publishedEvent.UpdatedAt.Should().Be(ClosedAt);
    }

    [Fact]
    public void Close_PublishedEvent_RaisesEventClosedEvent()
    {
        var publishedEvent = CreatePublishedEvent();

        publishedEvent.Close(ClosedAt);

        publishedEvent.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<EventClosedEvent>()
            .Which.ClosedAt.Should().Be(ClosedAt);
    }

    [Fact]
    public void Close_PublishedEvent_DoesNotSetCancelledAt()
    {
        var publishedEvent = CreatePublishedEvent();

        publishedEvent.Close(ClosedAt);

        publishedEvent.CancelledAt.Should().BeNull();
    }

    [Fact]
    public void Close_DraftEvent_ThrowsBusinessRuleValidationException()
    {
        var draftEvent = CreateDraftEvent();

        var act = () => draftEvent.Close(ClosedAt);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("EVENT_NOT_CLOSABLE");
    }

    [Fact]
    public void Close_AlreadyClosedEvent_ThrowsBusinessRuleValidationException()
    {
        var closedEvent = Event.FromPersistence(
            EventId.From(1),
            UserId.New(),
            EventTitle.Create("Closed Event"),
            EventSchedule.Create(StartsAt, EndsAt, "UTC"),
            EventLocation.Create("123 Main St", false),
            null,
            EventStatus.Closed,
            Slug.Create("closed-event-a1b2c3d4"),
            null,
            null,
            CreatedAt,
            ClosedAt,
            1);

        var act = () => closedEvent.Close(ClosedAt);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("EVENT_NOT_CLOSABLE");
    }

    [Fact]
    public void Close_CancelledEvent_ThrowsBusinessRuleValidationException()
    {
        var cancelledEvent = Event.FromPersistence(
            EventId.From(1),
            UserId.New(),
            EventTitle.Create("Cancelled Event"),
            EventSchedule.Create(StartsAt, EndsAt, "UTC"),
            EventLocation.Create("123 Main St", false),
            null,
            EventStatus.Cancelled,
            null,
            null,
            ClosedAt,
            CreatedAt,
            ClosedAt,
            1);

        var act = () => cancelledEvent.Close(ClosedAt);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("EVENT_NOT_CLOSABLE");
    }

    [Fact]
    public void Close_PublishedEvent_DoesNotAffectSlug()
    {
        var publishedEvent = CreatePublishedEvent();
        var originalSlug = publishedEvent.Slug;

        publishedEvent.Close(ClosedAt);

        publishedEvent.Slug.Should().Be(originalSlug);
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
        var draftEvent = CreateDraftEvent();
        draftEvent.AddTicketType(
            TicketName.Create("General Admission"),
            Money.Create(50m, "VND"),
            Capacity.Create(100),
            null,
            null,
            CreatedAt);
        draftEvent.Publish(Slug.Create("tech-conference-2026-a1b2c3d4"), PublishedAt);
        draftEvent.ClearDomainEvents();
        return draftEvent;
    }
}
