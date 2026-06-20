namespace EventHub.Contracts.Events;

public sealed record AssignRoleRequest(
    Guid UserId,
    string Role);
