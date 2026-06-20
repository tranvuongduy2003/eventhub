using EventHub.Domain.Users;

namespace EventHub.Domain.Events;

public sealed class PermissionAuditEntry
{
    private PermissionAuditEntry()
    {
    }

    public AuditEntryId Id { get; private set; }

    public EventId EventId { get; private set; }

    public UserId ActorId { get; private set; }

    public UserId TargetId { get; private set; }

    public AuditAction Action { get; private set; }

    public EventRole? OldRole { get; private set; }

    public EventRole? NewRole { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public static PermissionAuditEntry Create(
        EventId eventId,
        UserId actorId,
        UserId targetId,
        AuditAction action,
        EventRole? oldRole,
        EventRole? newRole,
        DateTimeOffset occurredAt) =>
        new()
        {
            EventId = eventId,
            ActorId = actorId,
            TargetId = targetId,
            Action = action,
            OldRole = oldRole,
            NewRole = newRole,
            OccurredAt = occurredAt,
        };
}
