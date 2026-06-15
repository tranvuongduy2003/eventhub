using EventHub.Domain.Abstractions;
using EventHub.Domain.Exceptions;

namespace EventHub.Domain.Users;

public sealed class PasswordHash : ValueObject
{
    private PasswordHash(string value) => Value = value;

    public string Value { get; }

    public static PasswordHash Create(string hashedValue)
    {
        if (string.IsNullOrWhiteSpace(hashedValue))
        {
            throw new BusinessRuleValidationException(
                "PASSWORD_HASH_REQUIRED",
                "Password hash is required.");
        }

        return new PasswordHash(hashedValue);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
