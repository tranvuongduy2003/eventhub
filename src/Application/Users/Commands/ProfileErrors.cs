using EventHub.Application.Common;

namespace EventHub.Application.Users.Commands;

public static class ProfileErrors
{
    public const string EmailTakenCode = "EMAIL_TAKEN";
    public const string InvalidFileTypeCode = "INVALID_FILE_TYPE";
    public const string FileTooLargeCode = "FILE_TOO_LARGE";

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

    public static readonly Error InvalidFileType = Error.Validation(
        InvalidFileTypeCode,
        "Only JPEG, PNG, and WebP images are supported.");

    public static readonly Error FileTooLarge = Error.Validation(
        FileTooLargeCode,
        "File size must not exceed 5 MB.");
}
