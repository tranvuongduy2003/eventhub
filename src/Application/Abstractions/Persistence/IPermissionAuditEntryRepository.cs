using EventHub.Application.Common;
using EventHub.Domain.Events;

namespace EventHub.Application.Abstractions.Persistence;

public interface IPermissionAuditEntryRepository
{
    Task AddAsync(PermissionAuditEntry entry, CancellationToken cancellationToken = default);

    Task<PaginatedResult<PermissionAuditEntry>> GetByEventAsync(
        EventId eventId,
        int page,
        int pageSize,
        DateTimeOffset? from,
        DateTimeOffset? to,
        AuditAction? action,
        CancellationToken cancellationToken = default);
}
