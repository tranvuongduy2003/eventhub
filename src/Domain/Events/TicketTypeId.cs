using EventHub.Domain.Exceptions;

namespace EventHub.Domain.Events;

public readonly record struct TicketTypeId(int Value)
{
    public static TicketTypeId From(int value)
    {
        if (value == 0)
        {
            throw new BusinessRuleValidationException(
                "TICKET_TYPE_ID_INVALID",
                "Ticket type id must not be zero.");
        }

        return new TicketTypeId(value);
    }

    public override string ToString() => Value.ToString();
}
