using Solution.Application.Abstractions.Services;

namespace Solution.Testing.Common.Fixtures;

public sealed class TestClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;
}
