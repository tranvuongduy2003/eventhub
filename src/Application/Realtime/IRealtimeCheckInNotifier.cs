using EventHub.Domain.Events;

namespace EventHub.Application.Realtime;

public interface IRealtimeCheckInNotifier
{
    Task NotifyCheckInChangedAsync(EventId eventId, CancellationToken cancellationToken = default);
}
