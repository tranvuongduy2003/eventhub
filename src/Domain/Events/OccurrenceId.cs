using EventHub.Domain.Exceptions;

namespace EventHub.Domain.Events;

public readonly record struct OccurrenceId(int Value)
{
    public static OccurrenceId From(int value)
    {
        if (value <= 0)
        {
            throw new BusinessRuleValidationException(
                "OCCURRENCE_ID_INVALID",
                "Occurrence id must be a positive integer.");
        }

        return new OccurrenceId(value);
    }

    public override string ToString() => Value.ToString();
}
