using EventHub.Application.Abstractions.Services;

namespace EventHub.Testing.Common.Fixtures;

public sealed class TestClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;
}
