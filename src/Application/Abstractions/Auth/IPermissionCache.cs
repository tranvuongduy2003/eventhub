using EventHub.Domain.Events;
using EventHub.Domain.Users;

namespace EventHub.Application.Abstractions.Auth;

public interface IPermissionCache
{
    Task<EventRole?> GetRoleAsync(EventId eventId, UserId userId, CancellationToken cancellationToken);
}
