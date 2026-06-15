namespace EventHub.Application.Users;

internal static class RegistrationValidationMessages
{
    public const string DisplayNameRequired = "Display name is required.";

    public const string DisplayNameLength = "Display name must be between 1 and 64 characters.";

    public const string EmailRequired = "Email is required.";

    public const string EmailTooLong = "Email cannot exceed 254 characters.";

    public const string EmailInvalid = "Email address format is invalid.";

    public const string PasswordRequired = "Password is required.";

    public const string PasswordTooShort = "Password must be at least 8 characters.";

    public const string PasswordMissingLetter = "Password must include at least one letter.";

    public const string PasswordMissingDigit = "Password must include at least one digit.";

    public const string PasswordMissingSpecial = "Password must include at least one special character.";

    // Keep in sync with Password.AllowedSpecialCharacters in EventHub.Domain.
    public const string AllowedPasswordSpecialCharacters = "!@#$%^&*()_+-=[]{}|;:'\",.<>?/\\`~";
}
