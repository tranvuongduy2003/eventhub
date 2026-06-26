using EventHub.Domain.Exceptions;

namespace EventHub.Domain.DiscountCodes;

public readonly record struct DiscountCodeId(int Value)
{
    public static DiscountCodeId From(int value)
    {
        if (value <= 0)
        {
            throw new BusinessRuleValidationException(
                "DISCOUNT_CODE_ID_INVALID",
                "Discount code id must be a positive integer.");
        }

        return new DiscountCodeId(value);
    }

    public override string ToString() => Value.ToString();
}
