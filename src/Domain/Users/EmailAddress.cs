using System.Net.Mail;
using EventHub.Domain.Abstractions;
using EventHub.Domain.Exceptions;

namespace EventHub.Domain.Users;

public sealed class EmailAddress : ValueObject
{
    private const int MaximumLength = 254;

    private EmailAddress(string normalizedValue, string displayValue)
    {
        Value = normalizedValue;
        DisplayValue = displayValue;
    }

    public string Value { get; }

    public string DisplayValue { get; }

    public static EmailAddress Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new BusinessRuleValidationException(
                "EMAIL_REQUIRED",
                "Email is required.");
        }

        var trimmed = value.Trim();

        if (trimmed.Length > MaximumLength)
        {
            throw new BusinessRuleValidationException(
                "EMAIL_TOO_LONG",
                $"Email cannot exceed {MaximumLength} characters.");
        }

        try
        {
            _ = new MailAddress(trimmed);
        }
        catch (FormatException)
        {
            throw new BusinessRuleValidationException(
                "EMAIL_INVALID",
                "Email address format is invalid.");
        }

        var normalized = trimmed.ToLowerInvariant();
        return new EmailAddress(normalized, trimmed);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
