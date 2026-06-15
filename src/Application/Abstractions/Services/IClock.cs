namespace EventHub.Application.Abstractions.Services;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
