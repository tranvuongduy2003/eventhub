using EventHub.Domain.Events;

namespace EventHub.Application.Realtime;

internal sealed class PendingRealtimeSalesInventoryUpdateCollector
    : IPendingRealtimeSalesInventoryUpdateCollector
{
    private readonly HashSet<EventId> _eventIds = [];

    public void Add(EventId eventId) => _eventIds.Add(eventId);

    public IReadOnlyCollection<EventId> Drain()
    {
        var eventIds = _eventIds.ToList();
        _eventIds.Clear();
        return eventIds;
    }
}
