using EventHub.Domain.Abstractions;
using EventHub.Domain.Exceptions;

namespace EventHub.Domain.Events;

public sealed class CoverImageRef : ValueObject
{
    private const int MaximumLength = 512;

    private CoverImageRef(string value) => Value = value;

    public string Value { get; }

    public static CoverImageRef Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new BusinessRuleValidationException(
                "COVER_IMAGE_REF_EMPTY",
                "Cover image reference cannot be empty.");
        }

        var trimmed = value.Trim();

        if (trimmed.Length > MaximumLength)
        {
            throw new BusinessRuleValidationException(
                "COVER_IMAGE_REF_TOO_LONG",
                $"Cover image reference cannot exceed {MaximumLength} characters.");
        }

        return new CoverImageRef(trimmed);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
