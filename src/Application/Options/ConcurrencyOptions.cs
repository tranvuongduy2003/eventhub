namespace EventHub.Application.Options;

public sealed class ConcurrencyOptions
{
    public const string SectionName = "Concurrency";

    public int MaxRetryAttempts { get; set; } = 3;

    public int BaseDelayMilliseconds { get; set; } = 25;
}
