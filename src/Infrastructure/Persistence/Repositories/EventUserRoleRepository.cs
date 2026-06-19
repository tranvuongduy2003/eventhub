using EventHub.Application.Abstractions.Persistence;
using EventHub.Domain.Events;
using EventHub.Domain.Users;
using EventHub.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Infrastructure.Persistence.Repositories;

internal sealed class EventUserRoleRepository(ApplicationDatabaseContext databaseContext) : IEventUserRoleRepository
{
    public async Task AddAsync(EventUserRole eventUserRole, CancellationToken cancellationToken = default) =>
        await databaseContext.EventUserRoles.AddAsync(EventUserRolePersistenceMapper.ToRecord(eventUserRole), cancellationToken);

    public async Task<EventUserRole?> GetByEventAndUserAsync(EventId eventId, UserId userId, CancellationToken cancellationToken = default)
    {
        var record = await databaseContext.EventUserRoles
            .AsNoTracking()
            .FirstOrDefaultAsync(
                eventUserRole => eventUserRole.EventId == eventId.Value && eventUserRole.UserId == userId.Value,
                cancellationToken);

        return record is null ? null : EventUserRolePersistenceMapper.ToDomain(record);
    }

    public async Task<IReadOnlyList<EventUserRole>> GetByEventAsync(EventId eventId, CancellationToken cancellationToken = default)
    {
        var records = await databaseContext.EventUserRoles
            .AsNoTracking()
            .Where(eventUserRole => eventUserRole.EventId == eventId.Value)
            .ToListAsync(cancellationToken);

        return records.Select(EventUserRolePersistenceMapper.ToDomain).ToList();
    }

    public Task<bool> ExistsByEventAndUserAsync(EventId eventId, UserId userId, CancellationToken cancellationToken = default) =>
        databaseContext.EventUserRoles
            .AsNoTracking()
            .AnyAsync(
                eventUserRole => eventUserRole.EventId == eventId.Value && eventUserRole.UserId == userId.Value,
                cancellationToken);

    public async Task UpdateRoleAsync(EventId eventId, UserId userId, EventRole newRole, CancellationToken cancellationToken = default)
    {
        var record = await databaseContext.EventUserRoles
            .AsTracking()
            .FirstOrDefaultAsync(
                eventUserRole => eventUserRole.EventId == eventId.Value && eventUserRole.UserId == userId.Value,
                cancellationToken);

        if (record is not null)
        {
            record.Role = newRole;
        }
    }

    public async Task DeleteByEventAndUserAsync(EventId eventId, UserId userId, CancellationToken cancellationToken = default)
    {
        var record = await databaseContext.EventUserRoles
            .AsTracking()
            .FirstOrDefaultAsync(
                eventUserRole => eventUserRole.EventId == eventId.Value && eventUserRole.UserId == userId.Value,
                cancellationToken);

        if (record is not null)
        {
            databaseContext.EventUserRoles.Remove(record);
        }
    }
}
