using Solution.Domain.Abstractions;
using Solution.Domain.Exceptions;

namespace Solution.Domain.Users;

public sealed class Username : ValueObject
{
    private const int MinimumLength = 3;
    private const int MaximumLength = 32;

    private Username(string value) => Value = value;

    public string Value { get; }

    public static Username Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new BusinessRuleValidationException(
                "USERNAME_REQUIRED",
                "Username is required.");
        }

        if (value.Length is < MinimumLength or > MaximumLength)
        {
            throw new BusinessRuleValidationException(
                "USERNAME_LENGTH",
                $"Username must be between {MinimumLength} and {MaximumLength} characters.");
        }

        if (!HasAllowedCharactersOnly(value))
        {
            throw new BusinessRuleValidationException(
                "USERNAME_INVALID_CHARACTERS",
                "Username may only contain letters, digits, and underscores.");
        }

        return new Username(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    private static bool HasAllowedCharactersOnly(string value)
    {
        foreach (char character in value)
        {
            if (!char.IsAsciiLetterOrDigit(character) && character != '_')
            {
                return false;
            }
        }

        return true;
    }
}
