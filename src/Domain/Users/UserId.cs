using EventHub.Domain.Exceptions;

namespace EventHub.Domain.Users;

public readonly record struct UserId(Guid Value)
{
    public static UserId New() => From(Guid.NewGuid());

    public static UserId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new BusinessRuleValidationException(
                "USER_ID_EMPTY",
                "User id cannot be empty.");
        }

        return new UserId(value);
    }

    public override string ToString() => Value.ToString("D");
}
