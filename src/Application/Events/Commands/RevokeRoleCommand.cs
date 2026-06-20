using EventHub.Application.Abstractions.Messaging;

namespace EventHub.Application.Events.Commands;

public sealed record RevokeRoleCommand(
    int EventId,
    Guid UserId) : ICommand;
