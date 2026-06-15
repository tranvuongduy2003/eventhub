namespace EventHub.Application.Abstractions.Auth;

public interface IPendingSessionCacheCollector
{
    void Enqueue(PendingSessionCacheEntry entry);

    IReadOnlyCollection<PendingSessionCacheEntry> Drain();
}

public sealed record PendingSessionCacheEntry(
    Guid SessionId,
    Guid UserId,
    DateTimeOffset ExpiresAt);
