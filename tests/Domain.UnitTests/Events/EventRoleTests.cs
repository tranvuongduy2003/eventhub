using EventHub.Domain.Events;
using FluentAssertions;

namespace EventHub.Domain.UnitTests.Events;

public sealed class EventRoleTests
{
    [Fact]
    public void Owner_HasAllFivePermissions()
    {
        var permissions = EventRolePermissions.GetPermissions(EventRole.Owner);

        permissions.Should().HaveCount(5);
        permissions.Should().Contain(Permission.EventManagement);
        permissions.Should().Contain(Permission.Ticketing);
        permissions.Should().Contain(Permission.CheckIn);
        permissions.Should().Contain(Permission.Reporting);
        permissions.Should().Contain(Permission.StaffManagement);
    }

    [Fact]
    public void Staff_HasExactlyCheckInAndReporting()
    {
        var permissions = EventRolePermissions.GetPermissions(EventRole.Staff);

        permissions.Should().HaveCount(2);
        permissions.Should().Contain(Permission.CheckIn);
        permissions.Should().Contain(Permission.Reporting);
    }

    [Fact]
    public void Staff_DoesNotHaveEventManagementTicketingOrStaffManagement()
    {
        var permissions = EventRolePermissions.GetPermissions(EventRole.Staff);

        permissions.Should().NotContain(Permission.EventManagement);
        permissions.Should().NotContain(Permission.Ticketing);
        permissions.Should().NotContain(Permission.StaffManagement);
    }

    [Fact]
    public void PermissionEnum_HasExactlyFiveValues()
    {
        var values = Enum.GetValues<Permission>();

        values.Should().HaveCount(5);
    }

    [Fact]
    public void Owner_PermissionsAutomaticallyIncludeNewPermissionValues()
    {
        var allPermissionValues = Enum.GetValues<Permission>();
        var ownerPermissions = EventRolePermissions.GetPermissions(EventRole.Owner);

        ownerPermissions.Should().BeEquivalentTo(allPermissionValues);
    }
}
