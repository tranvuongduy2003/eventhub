using EventHub.Domain.Events;

namespace EventHub.Application.Realtime;

public interface IPendingRealtimeSalesInventoryUpdateCollector
{
    void Add(EventId eventId);

    IReadOnlyCollection<EventId> Drain();
}
