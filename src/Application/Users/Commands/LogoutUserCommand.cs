using EventHub.Application.Abstractions.Messaging;

namespace EventHub.Application.Users.Commands;

public sealed record LogoutUserCommand(Guid SessionId, Guid UserId) : ICommand;
