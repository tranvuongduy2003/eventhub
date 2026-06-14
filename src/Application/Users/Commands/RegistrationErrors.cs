using Solution.Application.Common;

namespace Solution.Application.Users.Commands;

public static class RegistrationErrors
{
    public const string UsernameTakenCode = "USERNAME_TAKEN";

    public const string EmailTakenCode = "EMAIL_TAKEN";

    public static readonly Error UsernameTaken = Error.Validation(
        UsernameTakenCode,
        "That username is already in use.");

    public static readonly Error EmailTaken = Error.Validation(
        EmailTakenCode,
        "An account with this email already exists.");
}
