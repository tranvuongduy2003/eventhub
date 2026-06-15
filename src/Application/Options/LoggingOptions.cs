namespace EventHub.Application.Options;

public sealed class LoggingOptions
{
    public const string SectionName = "Logging";

    public string MinimumLevel { get; set; } = "Information";
}
