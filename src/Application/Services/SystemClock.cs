using EventHub.Application.Abstractions.Services;

namespace EventHub.Application.Services;

internal sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
