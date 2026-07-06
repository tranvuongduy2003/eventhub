using EventHub.Domain.Exceptions;

namespace EventHub.Domain.Payments;

public readonly record struct PaymentId(int Value)
{
    public static PaymentId From(int value)
    {
        if (value <= 0)
        {
            throw new BusinessRuleValidationException(
                "PAYMENT_ID_INVALID",
                "Payment id must be a positive integer.");
        }

        return new PaymentId(value);
    }

    public override string ToString() => Value.ToString();
}
