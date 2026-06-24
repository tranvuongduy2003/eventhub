using System.Text.RegularExpressions;
using EventHub.Domain.Abstractions;
using EventHub.Domain.Exceptions;

namespace EventHub.Domain.Orders;

public sealed class Contact : ValueObject
{
    private static readonly Regex EmailPattern = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private Contact()
    {
    }

    public string Name { get; private set; } = null!;

    public string Email { get; private set; } = null!;

    public static Contact Create(string name, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new BusinessRuleValidationException(
                "CONTACT_NAME_REQUIRED",
                "Contact name is required.");
        }

        var trimmedName = name.Trim();

        if (trimmedName.Length > 200)
        {
            throw new BusinessRuleValidationException(
                "CONTACT_NAME_TOO_LONG",
                "Contact name must not exceed 200 characters.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new BusinessRuleValidationException(
                "CONTACT_EMAIL_REQUIRED",
                "Contact email is required.");
        }

        var trimmedEmail = email.Trim();

        if (!EmailPattern.IsMatch(trimmedEmail))
        {
            throw new BusinessRuleValidationException(
                "CONTACT_EMAIL_INVALID",
                "Contact email is not a valid email address.");
        }

        return new Contact
        {
            Name = trimmedName,
            Email = trimmedEmail.ToLowerInvariant(),
        };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Name;
        yield return Email;
    }
}
