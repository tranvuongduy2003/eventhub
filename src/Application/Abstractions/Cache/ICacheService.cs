namespace EventHub.Application.Abstractions.Cache;

public interface ICacheService
{
    Task<bool> KeyExistsAsync(string key, CancellationToken cancellationToken = default);

    Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default);

    Task SetAsync(
        string key,
        string value,
        TimeSpan expiry,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}
