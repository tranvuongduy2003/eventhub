using EventHub.Application.Abstractions.Auth;

namespace EventHub.Application.Services;

internal sealed class PendingSessionCacheCollector : IPendingSessionCacheCollector
{
    private readonly List<PendingSessionCacheEntry> _entries = [];

    public void Enqueue(PendingSessionCacheEntry entry) => _entries.Add(entry);

    public IReadOnlyCollection<PendingSessionCacheEntry> Drain()
    {
        if (_entries.Count == 0)
        {
            return [];
        }

        var drained = _entries.ToArray();
        _entries.Clear();
        return drained;
    }
}
