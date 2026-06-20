namespace EventHub.Contracts.Events;

public sealed record EventRoleAssignmentResponse(
    Guid UserId,
    string DisplayName,
    string Email,
    string Role);
