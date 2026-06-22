using EventHub.Domain.Events;
using EventHub.Domain.Users;
using FluentAssertions;

namespace EventHub.Domain.UnitTests.Events;

public sealed class EventDraftTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset StartsAt = new(2026, 7, 15, 14, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset EndsAt = new(2026, 7, 15, 16, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateDraft_WithValidInput_CreatesEventInDraftStatus()
    {
        var draftEvent = CreateValidDraftEvent();

        draftEvent.Status.Should().Be(EventStatus.Draft);
        draftEvent.Id.Value.Should().Be(0);
        draftEvent.Title.Value.Should().Be("Tech Conference 2026");
    }

    [Fact]
    public void CreateDraft_WithValidInput_SetsOrganizerId()
    {
        var organizerId = UserId.New();
        var draftEvent = CreateDraftEvent(organizerId);

        draftEvent.OrganizerId.Should().Be(organizerId);
    }

    [Fact]
    public void CreateDraft_WithValidInput_SetsScheduleAndLocation()
    {
        var draftEvent = CreateValidDraftEvent();

        draftEvent.Schedule.StartsAt.Should().Be(StartsAt);
        draftEvent.Schedule.EndsAt.Should().Be(EndsAt);
        draftEvent.Schedule.TimeZoneId.Should().Be("UTC");
        draftEvent.Location.PhysicalAddress.Should().Be("123 Conference Ave");
        draftEvent.Location.IsOnline.Should().BeFalse();
    }

    [Fact]
    public void CreateDraft_WithValidInput_SetsTimestamps()
    {
        var draftEvent = CreateValidDraftEvent();

        draftEvent.CreatedAt.Should().Be(CreatedAt);
        draftEvent.UpdatedAt.Should().Be(CreatedAt);
    }

    [Fact]
    public void CreateDraft_WithValidInput_DoesNotRaiseEventCreatedEvent()
    {
        var draftEvent = CreateValidDraftEvent();

        draftEvent.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void FromPersistence_DoesNotRaiseEvents()
    {
        var draftEvent = Event.FromPersistence(
            EventId.From(1),
            UserId.New(),
            EventTitle.Create("Test"),
            EventSchedule.Create(StartsAt, EndsAt, "UTC"),
            EventLocation.Create("123 Main St", false),
            null,
            EventStatus.Draft,
            null,
            CreatedAt,
            CreatedAt,
            1);

        draftEvent.DomainEvents.Should().BeEmpty();
    }

    private static Event CreateValidDraftEvent() =>
        CreateDraftEvent(UserId.New());

    private static Event CreateDraftEvent(UserId organizerId) =>
        Event.CreateDraft(
            organizerId,
            EventTitle.Create("Tech Conference 2026"),
            EventSchedule.Create(StartsAt, EndsAt, "UTC"),
            EventLocation.Create("123 Conference Ave", false),
            CreatedAt);
}
