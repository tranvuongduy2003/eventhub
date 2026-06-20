using EventHub.Application.Abstractions.Messaging;

namespace EventHub.Application.Events.Commands;

public sealed record AcceptInvitationCommand(
    int InvitationId,
    string Token) : ICommand<AcceptInvitationResult>;

public sealed record AcceptInvitationResult(
    int InvitationId,
    int EventId,
    string Email,
    string Status);
