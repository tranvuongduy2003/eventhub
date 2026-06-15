namespace EventHub.Application.Options;

public sealed class AuthSessionOptions
{
    public const string SectionName = "Session";

    public string CookieName { get; set; } = "EventHub.Session";

    public int ExpirationHours { get; set; } = 24;
}
