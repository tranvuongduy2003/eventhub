using EventHub.Domain.Exceptions;

namespace EventHub.Domain.Orders;

public readonly record struct OrderId(int Value)
{
    public static OrderId From(int value)
    {
        if (value <= 0)
        {
            throw new BusinessRuleValidationException(
                "ORDER_ID_INVALID",
                "Order id must be a positive integer.");
        }

        return new OrderId(value);
    }

    public override string ToString() => Value.ToString();
}
