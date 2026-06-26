using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Common;
using EventHub.Domain.Events;
using EventHub.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Infrastructure.Persistence.Repositories;

internal sealed class PermissionAuditEntryRepository(ApplicationDatabaseContext databaseContext)
    : IPermissionAuditEntryRepository
{
    public async Task AddAsync(PermissionAuditEntry entry, CancellationToken cancellationToken = default) =>
        await databaseContext.PermissionAuditEntries.AddAsync(
            PermissionAuditEntryPersistenceMapper.ToRecord(entry), cancellationToken);

    public async Task<PaginatedResult<PermissionAuditEntry>> GetByEventAsync(
        EventId eventId,
        int page,
        int pageSize,
        DateTimeOffset? from,
        DateTimeOffset? to,
        AuditAction? action,
        CancellationToken cancellationToken = default)
    {
        var query = databaseContext.PermissionAuditEntries
            .AsNoTracking()
            .Where(entry => entry.EventId == eventId.Value);

        if (from.HasValue)
        {
            query = query.Where(entry => entry.OccurredAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(entry => entry.OccurredAt <= to.Value);
        }

        if (action.HasValue)
        {
            query = query.Where(entry => entry.Action == action.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var records = await query
            .OrderByDescending(entry => entry.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = records.Select(PermissionAuditEntryPersistenceMapper.ToDomain).ToList();

        return new PaginatedResult<PermissionAuditEntry>(items, totalCount);
    }
}
