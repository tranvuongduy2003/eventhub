using EventHub.Domain.Events;
using EventHub.Domain.Users;
using FluentAssertions;

namespace EventHub.Domain.UnitTests.Events;

public sealed class EventDuplicateTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset StartsAt = new(2026, 7, 15, 14, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset EndsAt = new(2026, 7, 15, 16, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PublishedAt = new(2026, 7, 2, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ClosedAt = new(2026, 7, 5, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset CancelledAt = new(2026, 7, 6, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset DuplicateAt = new(2026, 7, 10, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Duplicate_WithValidEvent_CreatesDraftEvent()
    {
        var sourceEvent = CreatePublishedEvent();
        var newOrganizerId = UserId.New();

        var duplicated = sourceEvent.Duplicate(newOrganizerId, DuplicateAt);

        duplicated.Status.Should().Be(EventStatus.Draft);
    }

    [Fact]
    public void Duplicate_WithValidEvent_CopiesTitleWithPrefix()
    {
        var sourceEvent = CreatePublishedEvent();
        var newOrganizerId = UserId.New();

        var duplicated = sourceEvent.Duplicate(newOrganizerId, DuplicateAt);

        duplicated.Title.Value.Should().Be("Copy of Tech Conference 2026");
    }

    [Fact]
    public void Duplicate_WithValidEvent_CopiesDescriptionLocationCoverImage()
    {
        var sourceEvent = CreateDraftEvent();
        sourceEvent.SetCoverImage(CoverImageRef.Create("covers/test.jpg"));
        var newOrganizerId = UserId.New();

        var duplicated = sourceEvent.Duplicate(newOrganizerId, DuplicateAt);

        duplicated.Description.Should().Be(sourceEvent.Description);
        duplicated.Location.Should().Be(sourceEvent.Location);
        duplicated.CoverImageRef.Should().Be(sourceEvent.CoverImageRef);
    }

    [Fact]
    public void Duplicate_WithValidEvent_SetsNewOrganizerId()
    {
        var sourceEvent = CreatePublishedEvent();
        var newOrganizerId = UserId.New();

        var duplicated = sourceEvent.Duplicate(newOrganizerId, DuplicateAt);

        duplicated.OrganizerId.Should().Be(newOrganizerId);
    }

    [Fact]
    public void Duplicate_WithValidEvent_ScheduleIsNull()
    {
        var sourceEvent = CreatePublishedEvent();
        var newOrganizerId = UserId.New();

        var duplicated = sourceEvent.Duplicate(newOrganizerId, DuplicateAt);

        duplicated.Schedule.Should().BeNull();
    }

    [Fact]
    public void Duplicate_WithValidEvent_SlugIsNull()
    {
        var sourceEvent = CreatePublishedEvent();
        var newOrganizerId = UserId.New();

        var duplicated = sourceEvent.Duplicate(newOrganizerId, DuplicateAt);

        duplicated.Slug.Should().BeNull();
    }

    [Fact]
    public void Duplicate_WithValidEvent_CancelledAtIsNull()
    {
        var sourceEvent = CreatePublishedEvent();
        var newOrganizerId = UserId.New();

        var duplicated = sourceEvent.Duplicate(newOrganizerId, DuplicateAt);

        duplicated.CancelledAt.Should().BeNull();
    }

    [Fact]
    public void Duplicate_WithValidEvent_DoesNotRaiseDomainEvents()
    {
        var sourceEvent = CreatePublishedEvent();
        var newOrganizerId = UserId.New();

        var duplicated = sourceEvent.Duplicate(newOrganizerId, DuplicateAt);

        duplicated.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Duplicate_WithValidEvent_SetsCreatedAtAndUpdatedAt()
    {
        var sourceEvent = CreatePublishedEvent();
        var newOrganizerId = UserId.New();

        var duplicated = sourceEvent.Duplicate(newOrganizerId, DuplicateAt);

        duplicated.CreatedAt.Should().Be(DuplicateAt);
        duplicated.UpdatedAt.Should().Be(DuplicateAt);
    }

    [Fact]
    public void Duplicate_WithValidEvent_SetsRowVersionToOne()
    {
        var sourceEvent = CreatePublishedEvent();
        var newOrganizerId = UserId.New();

        var duplicated = sourceEvent.Duplicate(newOrganizerId, DuplicateAt);

        duplicated.RowVersion.Should().Be(1);
    }

    [Fact]
    public void Duplicate_FromPublishedEvent_Succeeds()
    {
        var sourceEvent = CreatePublishedEvent();
        var newOrganizerId = UserId.New();

        var duplicated = sourceEvent.Duplicate(newOrganizerId, DuplicateAt);

        duplicated.Status.Should().Be(EventStatus.Draft);
    }

    [Fact]
    public void Duplicate_FromClosedEvent_Succeeds()
    {
        var publishedEvent = CreatePublishedEvent();
        publishedEvent.Close(ClosedAt);
        publishedEvent.ClearDomainEvents();
        var newOrganizerId = UserId.New();

        var duplicated = publishedEvent.Duplicate(newOrganizerId, DuplicateAt);

        duplicated.Status.Should().Be(EventStatus.Draft);
    }

    [Fact]
    public void Duplicate_FromCancelledEvent_Succeeds()
    {
        var publishedEvent = CreatePublishedEvent();
        publishedEvent.Cancel(CancelledAt);
        publishedEvent.ClearDomainEvents();
        var newOrganizerId = UserId.New();

        var duplicated = publishedEvent.Duplicate(newOrganizerId, DuplicateAt);

        duplicated.Status.Should().Be(EventStatus.Draft);
    }

    [Fact]
    public void Duplicate_FromDraftEvent_Succeeds()
    {
        var sourceEvent = CreateDraftEvent();
        var newOrganizerId = UserId.New();

        var duplicated = sourceEvent.Duplicate(newOrganizerId, DuplicateAt);

        duplicated.Status.Should().Be(EventStatus.Draft);
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
