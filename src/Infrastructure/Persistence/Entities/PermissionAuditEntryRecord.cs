using EventHub.Domain.Events;

namespace EventHub.Infrastructure.Persistence.Entities;

public sealed class PermissionAuditEntryRecord
{
    public int Id { get; set; }

    public int EventId { get; set; }

    public Guid ActorId { get; set; }

    public Guid TargetId { get; set; }

    public AuditAction Action { get; set; }

    public EventRole? OldRole { get; set; }

    public EventRole? NewRole { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public UserRecord? Actor { get; set; }

    public UserRecord? Target { get; set; }
}
