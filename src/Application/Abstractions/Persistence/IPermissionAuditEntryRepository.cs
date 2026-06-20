using EventHub.Domain.Events;

namespace EventHub.Application.Abstractions.Persistence;

public interface IPermissionAuditEntryRepository
{
    Task AddAsync(PermissionAuditEntry entry, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<PermissionAuditEntry> Items, int TotalCount)> GetByEventAsync(
        EventId eventId,
        int page,
        int pageSize,
        DateTimeOffset? from,
        DateTimeOffset? to,
        AuditAction? action,
        CancellationToken cancellationToken = default);
}
