using EventHub.Domain.Events;
using EventHub.Domain.Users;
using FluentAssertions;

namespace EventHub.Domain.UnitTests.Events;

public sealed class EventUserRoleTests
{
    [Fact]
    public void Create_AssignsCorrectProperties()
    {
        var eventId = EventId.From(1);
        var userId = UserId.From(Guid.NewGuid());
        var createdAt = DateTimeOffset.UtcNow;

        var assignment = EventUserRole.Create(eventId, userId, EventRole.Owner, createdAt);

        assignment.EventId.Should().Be(eventId);
        assignment.UserId.Should().Be(userId);
        assignment.Role.Should().Be(EventRole.Owner);
        assignment.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void ChangeRole_UpdatesRole()
    {
        var assignment = EventUserRole.Create(
            EventId.From(1),
            UserId.From(Guid.NewGuid()),
            EventRole.Owner,
            DateTimeOffset.UtcNow);

        assignment.ChangeRole(EventRole.Staff);

        assignment.Role.Should().Be(EventRole.Staff);
    }

    [Fact]
    public void ChangeRole_CanSwitchBetweenAllRoles()
    {
        var assignment = EventUserRole.Create(
            EventId.From(1),
            UserId.From(Guid.NewGuid()),
            EventRole.Staff,
            DateTimeOffset.UtcNow);

        assignment.ChangeRole(EventRole.Owner);
        assignment.Role.Should().Be(EventRole.Owner);

        assignment.ChangeRole(EventRole.Staff);
        assignment.Role.Should().Be(EventRole.Staff);
    }
}
