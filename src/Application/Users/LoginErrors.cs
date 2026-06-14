using Solution.Application.Common;

namespace Solution.Application.Users;

public static class LoginErrors
{
    public const string InvalidCredentialsCode = "INVALID_CREDENTIALS";

    public static readonly Error InvalidCredentials = Error.Unauthorized(
        InvalidCredentialsCode,
        "Email or password is incorrect.");
}
