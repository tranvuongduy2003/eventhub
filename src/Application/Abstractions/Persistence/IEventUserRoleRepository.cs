using EventHub.Domain.Events;
using EventHub.Domain.Users;

namespace EventHub.Application.Abstractions.Persistence;

public interface IEventUserRoleRepository
{
    Task AddAsync(EventUserRole eventUserRole, CancellationToken cancellationToken = default);

    Task<EventUserRole?> GetByEventAndUserAsync(EventId eventId, UserId userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EventUserRole>> GetByEventAsync(EventId eventId, CancellationToken cancellationToken = default);

    Task<bool> ExistsByEventAndUserAsync(EventId eventId, UserId userId, CancellationToken cancellationToken = default);

    Task UpdateRoleAsync(EventId eventId, UserId userId, EventRole newRole, CancellationToken cancellationToken = default);

    Task DeleteByEventAndUserAsync(EventId eventId, UserId userId, CancellationToken cancellationToken = default);
}
