using Solution.Application.Abstractions.Auth;
using Solution.Domain.Users;

namespace Solution.Api.IntegrationTests.Users.Fakes;

internal sealed class ThrowOnCreateSessionStore : ISessionStore
{
    public Task<SessionCreationResult> CreateSessionAsync(
        UserId userId,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Simulated session persistence failure.");

    public Task<UserId?> ResolveUserIdAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
        Task.FromResult<UserId?>(null);

    public Task TryWriteCacheAsync(PendingSessionCacheEntry entry, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task RevokeSessionAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
