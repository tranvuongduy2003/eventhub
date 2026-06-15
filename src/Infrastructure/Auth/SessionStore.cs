using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Abstractions.Cache;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Options;
using EventHub.Domain.Users;
using EventHub.Infrastructure.Persistence;
using EventHub.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventHub.Infrastructure.Auth;

internal sealed class SessionStore(
    ApplicationDatabaseContext databaseContext,
    IClock clock,
    IOptions<AuthSessionOptions> sessionOptions,
    ICacheService cacheService,
    ILogger<SessionStore> logger) : ISessionStore
{
    private static string SessionKey(Guid sessionId) => $"session:{sessionId:D}";

    public async Task<SessionCreationResult> CreateSessionAsync(
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        var sessionId = Guid.NewGuid();
        var createdAt = clock.UtcNow;
        var expiresAt = createdAt.AddHours(sessionOptions.Value.ExpirationHours);

        var record = new UserSessionRecord
        {
            Id = sessionId,
            UserId = userId.Value,
            CreatedAt = createdAt,
            LastSeenAt = createdAt,
            ExpiresAt = expiresAt,
        };

        await databaseContext.UserSessions.AddAsync(record, cancellationToken);

        return new SessionCreationResult(sessionId, expiresAt);
    }

    public async Task<UserId?> ResolveUserIdAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var cacheHadEntry = await cacheService.KeyExistsAsync(SessionKey(sessionId), cancellationToken);

        var now = clock.UtcNow;
        var session = await databaseContext.UserSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                userSession =>
                    userSession.Id == sessionId
                    && userSession.RevokedAt == null
                    && userSession.ExpiresAt > now,
                cancellationToken);

        if (session is not null)
        {
            return UserId.From(session.UserId);
        }

        if (cacheHadEntry)
        {
            await cacheService.DeleteAsync(SessionKey(sessionId), cancellationToken);
        }

        return null;
    }

    public async Task TryWriteCacheAsync(
        PendingSessionCacheEntry entry,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var timeToLive = entry.ExpiresAt - DateTimeOffset.UtcNow;
            if (timeToLive <= TimeSpan.Zero)
            {
                return;
            }

            await cacheService.SetAsync(
                SessionKey(entry.SessionId),
                entry.UserId.ToString("D"),
                timeToLive,
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Failed to write session {SessionId} to cache",
                entry.SessionId);
        }
    }

    public async Task RevokeSessionAsync(
        Guid sessionId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var revokedAt = clock.UtcNow;

        await databaseContext.UserSessions
            .Where(
                userSession =>
                    userSession.Id == sessionId
                    && userSession.UserId == userId
                    && userSession.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(userSession => userSession.RevokedAt, revokedAt),
                cancellationToken);

        try
        {
            await cacheService.DeleteAsync(SessionKey(sessionId), cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Failed to delete session {SessionId} from cache",
                sessionId);
        }
    }
}
