namespace Solution.Application.Users;

internal static class RegistrationValidationMessages
{
    public const string UsernameRequired = "Username is required.";

    public const string UsernameLength = "Username must be between 3 and 32 characters.";

    public const string UsernameInvalidCharacters =
        "Username may only contain letters, digits, and underscores.";

    public const string EmailRequired = "Email is required.";

    public const string EmailTooLong = "Email cannot exceed 254 characters.";

    public const string EmailInvalid = "Email address format is invalid.";

    public const string PasswordRequired = "Password is required.";

    public const string PasswordTooShort = "Password must be at least 8 characters.";

    public const string PasswordMissingLetter = "Password must include at least one letter.";

    public const string PasswordMissingDigit = "Password must include at least one digit.";

    public const string PasswordMissingSpecial = "Password must include at least one special character.";

    // Keep in sync with Password.AllowedSpecialCharacters in Solution.Domain.
    public const string AllowedPasswordSpecialCharacters = "!@#$%^&*()_+-=[]{}|;:'\",.<>?/\\`~";
}
