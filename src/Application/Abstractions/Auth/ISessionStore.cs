using EventHub.Domain.Users;

namespace EventHub.Application.Abstractions.Auth;

public interface ISessionStore
{
    Task<SessionCreationResult> CreateSessionAsync(UserId userId, CancellationToken cancellationToken = default);

    Task<UserId?> ResolveUserIdAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task TryWriteCacheAsync(PendingSessionCacheEntry entry, CancellationToken cancellationToken = default);

    Task RevokeSessionAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken = default);
}
