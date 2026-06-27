using EventHub.Domain.Events;
using EventHub.Domain.Exceptions;
using EventHub.Domain.Users;
using FluentAssertions;

namespace EventHub.Domain.UnitTests.Events;

public sealed class EventCancelTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset StartsAt = new(2026, 7, 15, 14, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset EndsAt = new(2026, 7, 15, 16, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PublishedAt = new(2026, 7, 2, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ClosedAt = new(2026, 7, 5, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset CancelledAt = new(2026, 7, 6, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Cancel_PublishedEvent_TransitionsToCancelledStatus()
    {
        var publishedEvent = CreatePublishedEvent();

        publishedEvent.Cancel(CancelledAt);

        publishedEvent.Status.Should().Be(EventStatus.Cancelled);
    }

    [Fact]
    public void Cancel_PublishedEvent_SetsCancelledAt()
    {
        var publishedEvent = CreatePublishedEvent();

        publishedEvent.Cancel(CancelledAt);

        publishedEvent.CancelledAt.Should().Be(CancelledAt);
    }

    [Fact]
    public void Cancel_PublishedEvent_UpdatesTimestamp()
    {
        var publishedEvent = CreatePublishedEvent();

        publishedEvent.Cancel(CancelledAt);

        publishedEvent.UpdatedAt.Should().Be(CancelledAt);
    }

    [Fact]
    public void Cancel_PublishedEvent_RaisesEventCancelledEvent()
    {
        var publishedEvent = CreatePublishedEvent();

        publishedEvent.Cancel(CancelledAt);

        publishedEvent.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<EventCancelledEvent>()
            .Which.CancelledAt.Should().Be(CancelledAt);
    }

    [Fact]
    public void Cancel_ClosedEvent_TransitionsToCancelledStatus()
    {
        var closedEvent = CreateClosedEvent();

        closedEvent.Cancel(CancelledAt);

        closedEvent.Status.Should().Be(EventStatus.Cancelled);
    }

    [Fact]
    public void Cancel_ClosedEvent_SetsCancelledAt()
    {
        var closedEvent = CreateClosedEvent();

        closedEvent.Cancel(CancelledAt);

        closedEvent.CancelledAt.Should().Be(CancelledAt);
    }

    [Fact]
    public void Cancel_ClosedEvent_RaisesEventCancelledEvent()
    {
        var closedEvent = CreateClosedEvent();

        closedEvent.Cancel(CancelledAt);

        closedEvent.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<EventCancelledEvent>()
            .Which.CancelledAt.Should().Be(CancelledAt);
    }

    [Fact]
    public void Cancel_DraftEvent_ThrowsBusinessRuleValidationException()
    {
        var draftEvent = CreateDraftEvent();

        var act = () => draftEvent.Cancel(CancelledAt);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("EVENT_NOT_CANCELLABLE");
    }

    [Fact]
    public void Cancel_AlreadyCancelledEvent_ThrowsBusinessRuleValidationException()
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
            CancelledAt,
            CreatedAt,
            CancelledAt,
            1);

        var act = () => cancelledEvent.Cancel(CancelledAt);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("EVENT_NOT_CANCELLABLE");
    }

    [Fact]
    public void Cancel_PublishedEvent_DoesNotAffectSlug()
    {
        var publishedEvent = CreatePublishedEvent();
        var originalSlug = publishedEvent.Slug;

        publishedEvent.Cancel(CancelledAt);

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

    private static Event CreateClosedEvent()
    {
        var publishedEvent = CreatePublishedEvent();
        publishedEvent.Close(ClosedAt);
        publishedEvent.ClearDomainEvents();
        return publishedEvent;
    }
}
