using EventHub.Domain.Events;
using EventHub.Domain.Exceptions;
using EventHub.Domain.Users;
using FluentAssertions;

namespace EventHub.Domain.UnitTests.Events;

public sealed class EventPublishTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset StartsAt = new(2026, 7, 15, 14, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset EndsAt = new(2026, 7, 15, 16, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PublishedAt = new(2026, 7, 2, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Publish_DraftEvent_TransitionsToPublishedStatus()
    {
        var draftEvent = CreateValidDraftEvent();
        var slug = Slug.Create("tech-conference-2026-a1b2c3d4");

        draftEvent.Publish(slug, PublishedAt);

        draftEvent.Status.Should().Be(EventStatus.Published);
    }

    [Fact]
    public void Publish_DraftEvent_SetsSlug()
    {
        var draftEvent = CreateValidDraftEvent();
        var slug = Slug.Create("tech-conference-2026-a1b2c3d4");

        draftEvent.Publish(slug, PublishedAt);

        draftEvent.Slug.Should().NotBeNull();
        draftEvent.Slug!.Value.Should().Be("tech-conference-2026-a1b2c3d4");
    }

    [Fact]
    public void Publish_DraftEvent_UpdatesTimestamp()
    {
        var draftEvent = CreateValidDraftEvent();
        var slug = Slug.Create("tech-conference-2026-a1b2c3d4");

        draftEvent.Publish(slug, PublishedAt);

        draftEvent.UpdatedAt.Should().Be(PublishedAt);
    }

    [Fact]
    public void Publish_DraftEvent_RaisesEventPublishedEvent()
    {
        var draftEvent = CreateValidDraftEvent();
        var slug = Slug.Create("tech-conference-2026-a1b2c3d4");

        draftEvent.Publish(slug, PublishedAt);

        draftEvent.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<EventPublishedEvent>()
            .Which.Slug.Value.Should().Be("tech-conference-2026-a1b2c3d4");
    }

    [Fact]
    public void Publish_AlreadyPublishedEvent_ThrowsBusinessRuleValidationException()
    {
        var draftEvent = CreateValidDraftEvent();
        var slug = Slug.Create("tech-conference-2026-a1b2c3d4");
        draftEvent.Publish(slug, PublishedAt);

        var act = () => draftEvent.Publish(Slug.Create("another-slug"), PublishedAt);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("EVENT_NOT_PUBLISHABLE");
    }

    [Fact]
    public void Publish_ClosedEvent_ThrowsBusinessRuleValidationException()
    {
        var draftEvent = CreateValidDraftEvent();
        var slug = Slug.Create("tech-conference-2026-a1b2c3d4");
        draftEvent.Publish(slug, PublishedAt);
        // Closed events cannot be created via domain methods yet,
        // so we test via FromPersistence to simulate a closed event.
        var closedEvent = Event.FromPersistence(
            EventId.From(1),
            UserId.New(),
            EventTitle.Create("Closed Event"),
            EventSchedule.Create(StartsAt, EndsAt, "UTC"),
            EventLocation.Create("123 Main St", false),
            null,
            EventStatus.Closed,
            null,
            null,
            null,
            CreatedAt,
            CreatedAt,
            1);

        var act = () => closedEvent.Publish(Slug.Create("closed-event-slug"), PublishedAt);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("EVENT_NOT_PUBLISHABLE");
    }

    [Fact]
    public void Publish_CancelledEvent_ThrowsBusinessRuleValidationException()
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
            null,
            CreatedAt,
            CreatedAt,
            1);

        var act = () => cancelledEvent.Publish(Slug.Create("cancelled-event-slug"), PublishedAt);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("EVENT_NOT_PUBLISHABLE");
    }

    private static Event CreateValidDraftEvent()
    {
        var draftEvent = Event.CreateDraft(
            UserId.New(),
            EventTitle.Create("Tech Conference 2026"),
            EventSchedule.Create(StartsAt, EndsAt, "UTC"),
            EventLocation.Create("123 Conference Ave", false),
            CreatedAt);

        draftEvent.AddTicketType(
            TicketName.Create("General Admission"),
            Money.Create(50m, "VND"),
            Capacity.Create(100),
            null,
            null,
            CreatedAt);

        draftEvent.ClearDomainEvents();

        return draftEvent;
    }
}
