using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Domain.Events;

namespace EventHub.Application.Events.Commands;

public sealed record AssignRoleCommand(
    int EventId,
    Guid UserId,
    string Role) : ICommand<AssignRoleResult>, IAuthorizeEventOperation
{
    EventId IAuthorizeEventOperation.EventId => Domain.Events.EventId.From(EventId);

    Permission IAuthorizeEventOperation.RequiredPermission => Permission.StaffManagement;
}

public sealed record AssignRoleResult(
    Guid UserId,
    string DisplayName,
    string Email,
    string Role);
