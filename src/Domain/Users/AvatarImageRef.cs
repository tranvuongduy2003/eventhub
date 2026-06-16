using EventHub.Domain.Abstractions;
using EventHub.Domain.Exceptions;

namespace EventHub.Domain.Users;

public sealed class AvatarImageRef : ValueObject
{
    private const int MaximumLength = 512;

    private AvatarImageRef(string value) => Value = value;

    public string Value { get; }

    public static AvatarImageRef Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new BusinessRuleValidationException(
                "AVATAR_IMAGE_REF_EMPTY",
                "Avatar image reference cannot be empty.");
        }

        var trimmed = value.Trim();

        if (trimmed.Length > MaximumLength)
        {
            throw new BusinessRuleValidationException(
                "AVATAR_IMAGE_REF_TOO_LONG",
                $"Avatar image reference cannot exceed {MaximumLength} characters.");
        }

        return new AvatarImageRef(trimmed);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
