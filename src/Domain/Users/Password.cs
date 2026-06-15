using EventHub.Domain.Abstractions;
using EventHub.Domain.Exceptions;

namespace EventHub.Domain.Users;

public sealed class Password : ValueObject
{
    private const int MinimumLength = 8;
    private const string AllowedSpecialCharacters = "!@#$%^&*()_+-=[]{}|;:'\",.<>?/\\`~";

    private Password(string value) => Value = value;

    public string Value { get; }

    public static Password Create(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
        {
            throw new BusinessRuleValidationException(
                "PASSWORD_REQUIRED",
                "Password is required.");
        }

        if (plaintext.Length < MinimumLength)
        {
            throw new BusinessRuleValidationException(
                "PASSWORD_TOO_SHORT",
                $"Password must be at least {MinimumLength} characters.");
        }

        if (!ContainsLetter(plaintext))
        {
            throw new BusinessRuleValidationException(
                "PASSWORD_MISSING_LETTER",
                "Password must include at least one letter.");
        }

        if (!ContainsDigit(plaintext))
        {
            throw new BusinessRuleValidationException(
                "PASSWORD_MISSING_DIGIT",
                "Password must include at least one digit.");
        }

        if (!ContainsSpecialCharacter(plaintext))
        {
            throw new BusinessRuleValidationException(
                "PASSWORD_MISSING_SPECIAL",
                "Password must include at least one special character.");
        }

        return new Password(plaintext);
    }

    public static Password ForCredentialVerification(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
        {
            throw new BusinessRuleValidationException(
                "PASSWORD_REQUIRED",
                "Password is required.");
        }

        return new Password(plaintext);
    }

    protected override IEnumerable<object?> GetEqualityComponents() => [];

    private static bool ContainsLetter(string value) =>
        value.Any(static character => char.IsAsciiLetter(character));

    private static bool ContainsDigit(string value) =>
        value.Any(static character => char.IsAsciiDigit(character));

    private static bool ContainsSpecialCharacter(string value) =>
        value.Any(static character => AllowedSpecialCharacters.Contains(character));
}
