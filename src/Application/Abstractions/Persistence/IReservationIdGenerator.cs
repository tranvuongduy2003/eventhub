using EventHub.Domain.Events;

namespace EventHub.Application.Abstractions.Persistence;

public interface IReservationIdGenerator
{
    Task<ReservationId> NextIdAsync(CancellationToken cancellationToken = default);
}
