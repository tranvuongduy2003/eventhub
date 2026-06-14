using Solution.Application.Abstractions.Messaging;

namespace Solution.Application.Users.Commands;

public sealed record LogoutUserCommand(Guid SessionId, Guid UserId) : ICommand;
