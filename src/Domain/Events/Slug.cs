using EventHub.Domain.Abstractions;
using EventHub.Domain.Exceptions;

namespace EventHub.Domain.Events;

public sealed class Slug : ValueObject
{
    private Slug()
    {
    }

    public string Value { get; private set; } = null!;

    public static Slug Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new BusinessRuleValidationException(
                "SLUG_EMPTY",
                "Slug must not be empty.");
        }

        return new Slug { Value = value };
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
