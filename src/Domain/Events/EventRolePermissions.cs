namespace EventHub.Domain.Events;

public static class EventRolePermissions
{
    private static readonly IReadOnlySet<Permission> OwnerPermissions =
        new HashSet<Permission>(Enum.GetValues<Permission>());

    private static readonly IReadOnlySet<Permission> StaffPermissions =
        new HashSet<Permission>([Permission.CheckIn, Permission.Reporting]);

    public static IReadOnlySet<Permission> GetPermissions(EventRole role) => role switch
    {
        EventRole.Owner => OwnerPermissions,
        EventRole.Staff => StaffPermissions,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown event role."),
    };
}
