using EventHub.Application.Common;

namespace EventHub.Application.Users.Commands;

public static class RegistrationErrors
{
    public const string EmailTakenCode = "EMAIL_TAKEN";

    private static readonly IReadOnlyDictionary<string, string[]> EmailTakenFieldErrors =
        new Dictionary<string, string[]>
        {
            ["email"] = ["An account with this email already exists."],
        };

    public static readonly Error EmailTaken = Error.Validation(
        EmailTakenCode,
        "An account with this email already exists.",
        new Dictionary<string, object?>
        {
            ["errors"] = EmailTakenFieldErrors,
        });
}
