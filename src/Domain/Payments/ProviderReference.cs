using EventHub.Domain.Abstractions;
using EventHub.Domain.Exceptions;

namespace EventHub.Domain.Payments;

public sealed class ProviderReference : ValueObject
{
    private ProviderReference(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static ProviderReference Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new BusinessRuleValidationException(
                "PAYMENT_PROVIDER_REFERENCE_REQUIRED",
                "A provider reference is required.");
        }

        if (value.Length > 200)
        {
            throw new BusinessRuleValidationException(
                "PAYMENT_PROVIDER_REFERENCE_TOO_LONG",
                "The provider reference must be 200 characters or fewer.");
        }

        return new ProviderReference(value.Trim());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
