using EventHub.Domain.Exceptions;

namespace EventHub.Domain.Events;

public readonly record struct ReservationId(int Value)
{
    public static ReservationId From(int value)
    {
        if (value <= 0)
        {
            throw new BusinessRuleValidationException(
                "RESERVATION_ID_INVALID",
                "Reservation id must be a positive integer.");
        }

        return new ReservationId(value);
    }

    public override string ToString() => Value.ToString();
}
