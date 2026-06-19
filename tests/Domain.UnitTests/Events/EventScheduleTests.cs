using EventHub.Domain.Events;
using EventHub.Domain.Exceptions;
using FluentAssertions;

namespace EventHub.Domain.UnitTests.Events;

public sealed class EventScheduleTests
{
    [Fact]
    public void EventSchedule_Create_EndsBeforeStart_Throws()
    {
        var startsAt = new DateTimeOffset(2026, 7, 1, 14, 0, 0, TimeSpan.Zero);
        var endsAt = new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);

        var act = () => EventSchedule.Create(startsAt, endsAt, "UTC");

        act.Should()
            .Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("EVENT_SCHEDULE_ENDS_BEFORE_START");
    }

    [Fact]
    public void EventSchedule_Create_EndsEqualsStart_Throws()
    {
        var startsAt = new DateTimeOffset(2026, 7, 1, 14, 0, 0, TimeSpan.Zero);
        var endsAt = startsAt;

        var act = () => EventSchedule.Create(startsAt, endsAt, "UTC");

        act.Should()
            .Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("EVENT_SCHEDULE_ENDS_BEFORE_START");
    }

    [Fact]
    public void EventSchedule_Create_ValidRange_CreatesInstance()
    {
        var startsAt = new DateTimeOffset(2026, 7, 1, 14, 0, 0, TimeSpan.Zero);
        var endsAt = new DateTimeOffset(2026, 7, 1, 16, 0, 0, TimeSpan.Zero);

        var schedule = EventSchedule.Create(startsAt, endsAt, "UTC");

        schedule.StartsAt.Should().Be(startsAt);
        schedule.EndsAt.Should().Be(endsAt);
        schedule.TimeZoneId.Should().Be("UTC");
    }
}
