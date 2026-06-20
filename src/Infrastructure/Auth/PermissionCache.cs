using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Domain.Events;
using EventHub.Domain.Users;

namespace EventHub.Infrastructure.Auth;

internal sealed class PermissionCache(IEventUserRoleRepository eventUserRoleRepository)
    : IPermissionCache
{
    private readonly Dictionary<(EventId, UserId), EventRole?> _cache = [];

    public async Task<EventRole?> GetRoleAsync(
        EventId eventId,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var key = (eventId, userId);

        if (_cache.TryGetValue(key, out var cachedRole))
        {
            return cachedRole;
        }

        var assignment = await eventUserRoleRepository.GetByEventAndUserAsync(
            eventId, userId, cancellationToken);

        var role = assignment?.Role;
        _cache[key] = role;

        return role;
    }
}
