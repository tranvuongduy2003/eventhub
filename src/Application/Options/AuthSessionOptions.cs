namespace Solution.Application.Options;

public sealed class AuthSessionOptions
{
    public const string SectionName = "Session";

    public string CookieName { get; set; } = "Boilerplate.Session";

    public int ExpirationHours { get; set; } = 24;
}
