using EventHub.Domain.Events;

namespace EventHub.Application.Realtime;

public interface IPendingRealtimeCheckInUpdateCollector
{
    void Add(EventId eventId);

    IReadOnlyCollection<EventId> Drain();
}
