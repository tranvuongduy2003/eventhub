using EventHub.Domain.Events;

namespace EventHub.Application.Realtime;

public interface IRealtimeSalesInventoryNotifier
{
    Task NotifySalesInventoryChangedAsync(EventId eventId, CancellationToken cancellationToken = default);
}
