using EventHub.Domain.Abstractions;
using EventHub.Domain.Exceptions;

namespace EventHub.Domain.Events;

public sealed class EventSchedule : ValueObject
{
    private EventSchedule(
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        string timeZoneId)
    {
        StartsAt = startsAt;
        EndsAt = endsAt;
        TimeZoneId = timeZoneId;
    }

    public DateTimeOffset StartsAt { get; }

    public DateTimeOffset EndsAt { get; }

    public string TimeZoneId { get; }

    public static EventSchedule Create(
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        string timeZoneId)
    {
        if (endsAt <= startsAt)
        {
            throw new BusinessRuleValidationException(
                "EVENT_SCHEDULE_ENDS_BEFORE_START",
                "Event end time must be after start time.");
        }

        return new EventSchedule(startsAt, endsAt, timeZoneId);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return StartsAt;
        yield return EndsAt;
        yield return TimeZoneId;
    }
}
