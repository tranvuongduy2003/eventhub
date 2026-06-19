using EventHub.Application.Abstractions.Messaging;

namespace EventHub.Application.Events.Commands;

public sealed record AssignRoleCommand(
    int EventId,
    Guid UserId,
    string Role) : ICommand<AssignRoleResult>;

public sealed record AssignRoleResult(
    Guid UserId,
    string DisplayName,
    string Email,
    string Role);
