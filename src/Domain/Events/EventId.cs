using EventHub.Domain.Exceptions;

namespace EventHub.Domain.Events;

public readonly record struct EventId(int Value)
{
    public static EventId From(int value)
    {
        if (value <= 0)
        {
            throw new BusinessRuleValidationException(
                "EVENT_ID_INVALID",
                "Event id must be a positive integer.");
        }

        return new EventId(value);
    }

    public override string ToString() => Value.ToString();
}
