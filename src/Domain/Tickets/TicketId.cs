using EventHub.Domain.Exceptions;

namespace EventHub.Domain.Tickets;

public readonly record struct TicketId(int Value)
{
    public static TicketId From(int value)
    {
        if (value <= 0)
        {
            throw new BusinessRuleValidationException(
                "TICKET_ID_INVALID",
                "Ticket id must be a positive integer.");
        }

        return new TicketId(value);
    }

    public override string ToString() => Value.ToString();
}
