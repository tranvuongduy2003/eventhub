using EventHub.Domain.Events;
using EventHub.Domain.Exceptions;
using EventHub.Domain.Users;
using FluentAssertions;

namespace EventHub.Domain.UnitTests.Events;

public sealed class OccurrenceTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset StartsAt = new(2026, 7, 15, 14, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset EndsAt = new(2026, 7, 15, 16, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset UpdatedAt = new(2026, 7, 2, 10, 0, 0, TimeSpan.Zero);

    // --- Occurrence.Schedule ---

    [Fact]
    public void Schedule_ValidInput_CreatesOccurrence()
    {
        var occurrence = Occurrence.Schedule(StartsAt, EndsAt, "Main Hall", "123 Main St", CreatedAt);

        occurrence.StartsAt.Should().Be(StartsAt);
        occurrence.EndsAt.Should().Be(EndsAt);
        occurrence.VenueName.Should().Be("Main Hall");
        occurrence.Address.Should().Be("123 Main St");
        occurrence.CreatedAt.Should().Be(CreatedAt);
        occurrence.UpdatedAt.Should().Be(CreatedAt);
    }

    [Fact]
    public void Schedule_EndsBeforeStart_ThrowsBusinessRuleValidationException()
    {
        var act = () => Occurrence.Schedule(EndsAt, StartsAt, null, null, CreatedAt);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("OCCURRENCE_ENDS_BEFORE_START");
    }

    [Fact]
    public void Schedule_EndsEqualsStart_ThrowsBusinessRuleValidationException()
    {
        var act = () => Occurrence.Schedule(StartsAt, StartsAt, null, null, CreatedAt);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("OCCURRENCE_ENDS_BEFORE_START");
    }

    // --- Event.ScheduleOccurrence ---

    [Fact]
    public void ScheduleOccurrence_ValidInput_AddsToCollection()
    {
        var draftEvent = CreateValidDraftEvent();

        var occurrence = draftEvent.ScheduleOccurrence(StartsAt, EndsAt, null, null, CreatedAt);

        draftEvent.Occurrences.Should().ContainSingle()
            .Which.Should().Be(occurrence);
    }

    [Fact]
    public void ScheduleOccurrence_ValidInput_RaisesOccurrenceScheduledEvent()
    {
        var draftEvent = CreateValidDraftEvent();

        draftEvent.ScheduleOccurrence(StartsAt, EndsAt, null, null, CreatedAt);

        draftEvent.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OccurrenceScheduledEvent>();
    }

    [Fact]
    public void ScheduleOccurrence_ValidInput_UpdatesEventTimestamp()
    {
        var draftEvent = CreateValidDraftEvent();

        draftEvent.ScheduleOccurrence(StartsAt, EndsAt, null, null, CreatedAt);

        draftEvent.UpdatedAt.Should().Be(CreatedAt);
    }

    [Fact]
    public void ScheduleOccurrence_OverlappingTimeRange_ThrowsBusinessRuleValidationException()
    {
        var draftEvent = CreateValidDraftEvent();
        draftEvent.ScheduleOccurrence(StartsAt, EndsAt, null, null, CreatedAt);

        // Overlapping: starts before existing ends, ends after existing starts
        var overlappingStart = StartsAt.AddHours(-1);
        var overlappingEnd = StartsAt.AddHours(1);

        var act = () => draftEvent.ScheduleOccurrence(overlappingStart, overlappingEnd, null, null, CreatedAt);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("OCCURRENCE_OVERLAPS");
    }

    [Fact]
    public void ScheduleOccurrence_NonOverlappingTimeRange_Succeeds()
    {
        var draftEvent = CreateValidDraftEvent();
        draftEvent.ScheduleOccurrence(StartsAt, EndsAt, null, null, CreatedAt);

        // Non-overlapping: starts after existing ends
        var laterStart = EndsAt.AddHours(1);
        var laterEnd = EndsAt.AddHours(3);

        var occurrence = draftEvent.ScheduleOccurrence(laterStart, laterEnd, null, null, CreatedAt);

        draftEvent.Occurrences.Should().HaveCount(2);
    }

    [Fact]
    public void ScheduleOccurrence_ClosedEvent_ThrowsBusinessRuleValidationException()
    {
        var closedEvent = CreateClosedEvent();

        var act = () => closedEvent.ScheduleOccurrence(StartsAt, EndsAt, null, null, CreatedAt);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("EVENT_CLOSED_OR_CANCELLED");
    }

    [Fact]
    public void ScheduleOccurrence_CancelledEvent_ThrowsBusinessRuleValidationException()
    {
        var cancelledEvent = CreateCancelledEvent();

        var act = () => cancelledEvent.ScheduleOccurrence(StartsAt, EndsAt, null, null, CreatedAt);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("EVENT_CLOSED_OR_CANCELLED");
    }

    // --- Event.RescheduleOccurrence ---

    [Fact]
    public void RescheduleOccurrence_ValidInput_UpdatesOccurrence()
    {
        var draftEvent = CreateValidDraftEvent();
        var occurrence = draftEvent.ScheduleOccurrence(StartsAt, EndsAt, null, null, CreatedAt);
        var newStartsAt = StartsAt.AddDays(1);
        var newEndsAt = EndsAt.AddDays(1);

        draftEvent.RescheduleOccurrence(occurrence.Id, newStartsAt, newEndsAt, "New Venue", "New Address", UpdatedAt);

        var updated = draftEvent.Occurrences.First(o => o.Id == occurrence.Id);
        updated.StartsAt.Should().Be(newStartsAt);
        updated.EndsAt.Should().Be(newEndsAt);
        updated.VenueName.Should().Be("New Venue");
        updated.Address.Should().Be("New Address");
    }

    [Fact]
    public void RescheduleOccurrence_ValidInput_RaisesOccurrenceUpdatedEvent()
    {
        var draftEvent = CreateValidDraftEvent();
        var occurrence = draftEvent.ScheduleOccurrence(StartsAt, EndsAt, null, null, CreatedAt);
        draftEvent.ClearDomainEvents();

        var newStartsAt = StartsAt.AddDays(1);
        var newEndsAt = EndsAt.AddDays(1);

        draftEvent.RescheduleOccurrence(occurrence.Id, newStartsAt, newEndsAt, null, null, UpdatedAt);

        draftEvent.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OccurrenceUpdatedEvent>();
    }

    [Fact]
    public void RescheduleOccurrence_NotFound_ThrowsBusinessRuleValidationException()
    {
        var draftEvent = CreateValidDraftEvent();

        var act = () => draftEvent.RescheduleOccurrence(
            OccurrenceId.From(999), StartsAt, EndsAt, null, null, UpdatedAt);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("OCCURRENCE_NOT_FOUND");
    }

    [Fact]
    public void RescheduleOccurrence_OverlappingWithOther_ThrowsBusinessRuleValidationException()
    {
        var draftEvent = CreateValidDraftEvent();

        // Use LoadOccurrences to give distinct IDs
        var laterStart = EndsAt.AddHours(1);
        var laterEnd = EndsAt.AddHours(3);
        draftEvent.LoadOccurrences(
        [
            Occurrence.FromPersistence(OccurrenceId.From(1), StartsAt, EndsAt, null, null, CreatedAt, CreatedAt),
            Occurrence.FromPersistence(OccurrenceId.From(2), laterStart, laterEnd, null, null, CreatedAt, CreatedAt),
        ]);

        // Try to reschedule second to overlap with first
        var act = () => draftEvent.RescheduleOccurrence(
            OccurrenceId.From(2), StartsAt.AddHours(-1), EndsAt.AddHours(1), null, null, UpdatedAt);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("OCCURRENCE_OVERLAPS");
    }

    [Fact]
    public void RescheduleOccurrence_ClosedEvent_ThrowsBusinessRuleValidationException()
    {
        var closedEvent = CreateClosedEventWithOccurrence();

        var act = () => closedEvent.RescheduleOccurrence(
            OccurrenceId.From(1), StartsAt, EndsAt, null, null, UpdatedAt);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("EVENT_CLOSED_OR_CANCELLED");
    }

    // --- Event.RemoveOccurrence ---

    [Fact]
    public void RemoveOccurrence_ExistingOccurrence_RemovesFromCollection()
    {
        var draftEvent = CreateValidDraftEvent();
        var occurrence = draftEvent.ScheduleOccurrence(StartsAt, EndsAt, null, null, CreatedAt);

        draftEvent.RemoveOccurrence(occurrence.Id, UpdatedAt);

        draftEvent.Occurrences.Should().BeEmpty();
    }

    [Fact]
    public void RemoveOccurrence_ExistingOccurrence_RaisesOccurrenceRemovedEvent()
    {
        var draftEvent = CreateValidDraftEvent();
        var occurrence = draftEvent.ScheduleOccurrence(StartsAt, EndsAt, null, null, CreatedAt);
        draftEvent.ClearDomainEvents();

        draftEvent.RemoveOccurrence(occurrence.Id, UpdatedAt);

        draftEvent.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OccurrenceRemovedEvent>()
            .Which.OccurrenceId.Should().Be(occurrence.Id);
    }

    [Fact]
    public void RemoveOccurrence_NotFound_ThrowsBusinessRuleValidationException()
    {
        var draftEvent = CreateValidDraftEvent();

        var act = () => draftEvent.RemoveOccurrence(OccurrenceId.From(999), UpdatedAt);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("OCCURRENCE_NOT_FOUND");
    }

    [Fact]
    public void RemoveOccurrence_ClosedEvent_ThrowsBusinessRuleValidationException()
    {
        var closedEvent = CreateClosedEventWithOccurrence();

        var act = () => closedEvent.RemoveOccurrence(OccurrenceId.From(1), UpdatedAt);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("EVENT_CLOSED_OR_CANCELLED");
    }

    [Fact]
    public void RemoveOccurrence_CancelledEvent_ThrowsBusinessRuleValidationException()
    {
        var cancelledEvent = CreateCancelledEventWithOccurrence();

        var act = () => cancelledEvent.RemoveOccurrence(OccurrenceId.From(1), UpdatedAt);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("EVENT_CLOSED_OR_CANCELLED");
    }

    // --- Event.LoadOccurrences ---

    [Fact]
    public void LoadOccurrences_SetsOccurrencesOnEvent()
    {
        var draftEvent = CreateValidDraftEvent();
        var occurrences = new List<Occurrence>
        {
            Occurrence.FromPersistence(
                OccurrenceId.From(1),
                StartsAt,
                EndsAt,
                null,
                null,
                CreatedAt,
                CreatedAt),
            Occurrence.FromPersistence(
                OccurrenceId.From(2),
                StartsAt.AddDays(7),
                EndsAt.AddDays(7),
                null,
                null,
                CreatedAt,
                CreatedAt),
        };

        draftEvent.LoadOccurrences(occurrences);

        draftEvent.Occurrences.Should().HaveCount(2);
    }

    [Fact]
    public void LoadOccurrences_ReplacesExistingOccurrences()
    {
        var draftEvent = CreateValidDraftEvent();
        draftEvent.ScheduleOccurrence(StartsAt, EndsAt, null, null, CreatedAt);

        var newOccurrences = new List<Occurrence>
        {
            Occurrence.FromPersistence(
                OccurrenceId.From(99),
                StartsAt.AddDays(30),
                EndsAt.AddDays(30),
                null,
                null,
                CreatedAt,
                CreatedAt),
        };

        draftEvent.LoadOccurrences(newOccurrences);

        draftEvent.Occurrences.Should().HaveCount(1);
        draftEvent.Occurrences.First().Id.Value.Should().Be(99);
    }

    // --- Occurrence.FromPersistence ---

    [Fact]
    public void FromPersistence_SetsAllProperties()
    {
        var occurrence = Occurrence.FromPersistence(
            OccurrenceId.From(42),
            StartsAt,
            EndsAt,
            "Venue",
            "Address",
            CreatedAt,
            UpdatedAt);

        occurrence.Id.Value.Should().Be(42);
        occurrence.StartsAt.Should().Be(StartsAt);
        occurrence.EndsAt.Should().Be(EndsAt);
        occurrence.VenueName.Should().Be("Venue");
        occurrence.Address.Should().Be("Address");
        occurrence.CreatedAt.Should().Be(CreatedAt);
        occurrence.UpdatedAt.Should().Be(UpdatedAt);
    }

    // --- Helper methods ---

    private static Event CreateValidDraftEvent() =>
        Event.CreateDraft(
            UserId.New(),
            EventTitle.Create("Tech Conference 2026"),
            EventSchedule.Create(StartsAt, EndsAt, "UTC"),
            EventLocation.Create("123 Conference Ave", false),
            CreatedAt);

    private static Event CreateClosedEvent() =>
        Event.FromPersistence(
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

    private static Event CreateCancelledEvent() =>
        Event.FromPersistence(
            EventId.From(1),
            UserId.New(),
            EventTitle.Create("Cancelled Event"),
            EventSchedule.Create(StartsAt, EndsAt, "UTC"),
            EventLocation.Create("123 Main St", false),
            null,
            EventStatus.Cancelled,
            null,
            null,
            CreatedAt,
            CreatedAt,
            CreatedAt,
            1);

    private static Event CreateClosedEventWithOccurrence()
    {
        var e = Event.FromPersistence(
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

        e.LoadOccurrences(
        [
            Occurrence.FromPersistence(
                OccurrenceId.From(1),
                StartsAt,
                EndsAt,
                null,
                null,
                CreatedAt,
                CreatedAt),
        ]);

        return e;
    }

    private static Event CreateCancelledEventWithOccurrence()
    {
        var e = Event.FromPersistence(
            EventId.From(1),
            UserId.New(),
            EventTitle.Create("Cancelled Event"),
            EventSchedule.Create(StartsAt, EndsAt, "UTC"),
            EventLocation.Create("123 Main St", false),
            null,
            EventStatus.Cancelled,
            null,
            null,
            CreatedAt,
            CreatedAt,
            CreatedAt,
            1);

        e.LoadOccurrences(
        [
            Occurrence.FromPersistence(
                OccurrenceId.From(1),
                StartsAt,
                EndsAt,
                null,
                null,
                CreatedAt,
                CreatedAt),
        ]);

        return e;
    }
}
