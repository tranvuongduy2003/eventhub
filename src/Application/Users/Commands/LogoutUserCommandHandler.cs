using Microsoft.Extensions.Logging;
using Solution.Application.Abstractions.Auth;
using Solution.Application.Abstractions.Messaging;
using Solution.Application.Common;

namespace Solution.Application.Users.Commands;

public sealed class LogoutUserCommandHandler(
    ISessionStore sessionStore,
    ILogger<LogoutUserCommandHandler> logger)
    : CommandHandler<LogoutUserCommand>
{
    public override async Task<Result> Handle(
        LogoutUserCommand command,
        CancellationToken cancellationToken)
    {
        await sessionStore.RevokeSessionAsync(
            command.SessionId,
            command.UserId,
            cancellationToken);

        logger.LogInformation(
            "UserLoggedOut {UserId} {SessionId}",
            command.UserId,
            command.SessionId);

        return Result.Success();
    }
}
