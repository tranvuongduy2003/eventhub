using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Common;
using Microsoft.Extensions.Logging;

namespace EventHub.Application.Users.Commands;

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
