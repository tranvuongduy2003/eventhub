using EventHub.Application.Abstractions.Cache;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace EventHub.Infrastructure.Cache;

internal sealed class CacheService(
    IConnectionMultiplexer connectionMultiplexer,
    ILogger<CacheService> logger) : ICacheService
{
    public async Task<bool> KeyExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await connectionMultiplexer
                .GetDatabase()
                .StringGetAsync(key);

            return value.HasValue;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Cache key lookup failed for {Key}", key);
            return false;
        }
    }

    public async Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await connectionMultiplexer
                .GetDatabase()
                .StringGetAsync(key);

            return value.HasValue ? value.ToString() : null;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Cache get failed for {Key}", key);
            return null;
        }
    }

    public async Task SetAsync(
        string key,
        string value,
        TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (expiry <= TimeSpan.Zero)
            {
                return;
            }

            await connectionMultiplexer
                .GetDatabase()
                .StringSetAsync(key, value, expiry);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Cache set failed for {Key}", key);
        }
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await connectionMultiplexer.GetDatabase().KeyDeleteAsync(key);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Cache delete failed for {Key}", key);
        }
    }
}
