namespace EventHub.Application.Options;

public sealed class RealtimeOptions
{
    public const string SectionName = "Realtime";

    public bool UseRedisBackplane { get; set; }
}
