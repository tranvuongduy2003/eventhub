using Solution.Application.Abstractions.Services;

namespace Solution.Application.Services;

internal sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
