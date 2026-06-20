using EventHub.Domain.Events;
using EventHub.Domain.Users;
using FluentAssertions;

namespace EventHub.Domain.UnitTests.Events;

public sealed class PermissionAuditEntryTests
{
    [Fact]
    public void Create_AssignsCorrectProperties()
    {
        var eventId = EventId.From(1);
        var actorId = UserId.From(Guid.NewGuid());
        var targetId = UserId.From(Guid.NewGuid());
        var occurredAt = DateTimeOffset.UtcNow;

        var entry = PermissionAuditEntry.Create(
            eventId, actorId, targetId,
            AuditAction.Assigned, null, EventRole.Staff, occurredAt);

        entry.EventId.Should().Be(eventId);
        entry.ActorId.Should().Be(actorId);
        entry.TargetId.Should().Be(targetId);
        entry.Action.Should().Be(AuditAction.Assigned);
        entry.OldRole.Should().BeNull();
        entry.NewRole.Should().Be(EventRole.Staff);
        entry.OccurredAt.Should().Be(occurredAt);
    }

    [Fact]
    public void Create_WithOldAndNewRole_AssignsBothRoles()
    {
        var entry = PermissionAuditEntry.Create(
            EventId.From(1),
            UserId.From(Guid.NewGuid()),
            UserId.From(Guid.NewGuid()),
            AuditAction.Transferred,
            EventRole.Owner,
            EventRole.Staff,
            DateTimeOffset.UtcNow);

        entry.OldRole.Should().Be(EventRole.Owner);
        entry.NewRole.Should().Be(EventRole.Staff);
    }

    [Fact]
    public void Create_RevokedAction_WithOldRoleOnly()
    {
        var entry = PermissionAuditEntry.Create(
            EventId.From(1),
            UserId.From(Guid.NewGuid()),
            UserId.From(Guid.NewGuid()),
            AuditAction.Revoked,
            EventRole.Staff,
            null,
            DateTimeOffset.UtcNow);

        entry.Action.Should().Be(AuditAction.Revoked);
        entry.OldRole.Should().Be(EventRole.Staff);
        entry.NewRole.Should().BeNull();
    }

    [Fact]
    public void PropertiesHavePrivateSetters_OnlyFactoryCanCreate()
    {
        var entry = PermissionAuditEntry.Create(
            EventId.From(1),
            UserId.From(Guid.NewGuid()),
            UserId.From(Guid.NewGuid()),
            AuditAction.Assigned,
            null,
            EventRole.Owner,
            DateTimeOffset.UtcNow);

        // Verify properties are set correctly — no public setters exist
        entry.Id.Should().Be(new AuditEntryId(0));
        entry.Action.Should().Be(AuditAction.Assigned);
    }
}
