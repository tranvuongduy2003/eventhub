using EventHub.Domain.Exceptions;

namespace EventHub.Domain.Events;

public readonly record struct InvitationId(int Value)
{
    public static InvitationId From(int value)
    {
        if (value <= 0)
        {
            throw new BusinessRuleValidationException(
                "INVITATION_ID_INVALID",
                "Invitation id must be a positive integer.");
        }
        return new InvitationId(value);
    }

    public override string ToString() => Value.ToString();
}
