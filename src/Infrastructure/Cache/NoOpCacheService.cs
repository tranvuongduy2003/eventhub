using EventHub.Application.Abstractions.Cache;

namespace EventHub.Infrastructure.Cache;

internal sealed class NoOpCacheService : ICacheService
{
    public Task<bool> KeyExistsAsync(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);

    public Task SetAsync(
        string key,
        string value,
        TimeSpan expiry,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
