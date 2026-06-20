using EventHub.Domain.Abstractions;
using EventHub.Domain.Exceptions;

namespace EventHub.Domain.Events;

public sealed class EventTitle : ValueObject
{
    private const int MinimumLength = 1;
    private const int MaximumLength = 200;

    private EventTitle(string value) => Value = value;

    public string Value { get; }

    public static EventTitle Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new BusinessRuleValidationException(
                "EVENT_TITLE_REQUIRED",
                "Event title is required.");
        }

        var trimmed = value.Trim();

        if (trimmed.Length is < MinimumLength or > MaximumLength)
        {
            throw new BusinessRuleValidationException(
                "EVENT_TITLE_TOO_LONG",
                $"Event title must be between {MinimumLength} and {MaximumLength} characters.");
        }

        return new EventTitle(trimmed);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
