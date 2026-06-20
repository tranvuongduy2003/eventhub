using EventHub.Domain.Events;
using EventHub.Domain.Users;
using EventHub.Infrastructure.Persistence.Entities;

namespace EventHub.Infrastructure.Persistence.Mapping;

internal static class PermissionAuditEntryPersistenceMapper
{
    public static PermissionAuditEntryRecord ToRecord(PermissionAuditEntry entry) =>
        new()
        {
            EventId = entry.EventId.Value,
            ActorId = entry.ActorId.Value,
            TargetId = entry.TargetId.Value,
            Action = entry.Action,
            OldRole = entry.OldRole,
            NewRole = entry.NewRole,
            OccurredAt = entry.OccurredAt,
        };

    public static PermissionAuditEntry ToDomain(PermissionAuditEntryRecord record) =>
        PermissionAuditEntry.Create(
            EventId.From(record.EventId),
            UserId.From(record.ActorId),
            UserId.From(record.TargetId),
            record.Action,
            record.OldRole,
            record.NewRole,
            record.OccurredAt);
}
