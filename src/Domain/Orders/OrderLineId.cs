using EventHub.Domain.Exceptions;

namespace EventHub.Domain.Orders;

public readonly record struct OrderLineId(int Value)
{
    public static OrderLineId From(int value)
    {
        if (value <= 0)
        {
            throw new BusinessRuleValidationException(
                "ORDER_LINE_ID_INVALID",
                "Order line id must be a positive integer.");
        }

        return new OrderLineId(value);
    }

    public override string ToString() => Value.ToString();
}
