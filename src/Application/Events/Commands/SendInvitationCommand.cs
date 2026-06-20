using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Domain.Events;

namespace EventHub.Application.Events.Commands;

public sealed record SendInvitationCommand(
    int EventId,
    string Email,
    int? ExpiresInDays) : ICommand<SendInvitationResult>, IAuthorizeEventOperation
{
    EventId IAuthorizeEventOperation.EventId => Domain.Events.EventId.From(EventId);

    Permission IAuthorizeEventOperation.RequiredPermission => Permission.StaffManagement;
}

public sealed record SendInvitationResult(
    int InvitationId,
    string Email,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    string Token);
