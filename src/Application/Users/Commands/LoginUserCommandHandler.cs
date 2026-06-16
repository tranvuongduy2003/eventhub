using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Common;
using EventHub.Application.Users;
using EventHub.Domain.Users;

namespace EventHub.Application.Users.Commands;

public sealed class LoginUserCommandHandler(
    IUserRepository userRepository,
    ISessionStore sessionStore,
    IPasswordHasher passwordHasher,
    IPendingSessionCacheCollector pendingSessionCacheCollector)
    : CommandHandler<LoginUserCommand, LoginUserResult>
{
    public override async Task<Result<LoginUserResult>> Handle(
        LoginUserCommand command,
        CancellationToken cancellationToken)
    {
        var email = EmailAddress.Create(command.Email);
        var password = Password.ForCredentialVerification(command.Password);

        var user = await userRepository.GetByEmailAsync(email.Value, cancellationToken);
        if (user is null || !passwordHasher.Verify(password, user.PasswordHash))
        {
            return LoginErrors.InvalidCredentials;
        }

        var session = await sessionStore.CreateSessionAsync(user.Id, cancellationToken);
        pendingSessionCacheCollector.Enqueue(
            new PendingSessionCacheEntry(
                session.SessionId,
                user.Id.Value,
                session.ExpiresAt));

        return new LoginUserResult(
            user.Id.Value,
            user.DisplayName.Value,
            email.DisplayValue,
            user.Role.ToString(),
            session.SessionId,
            session.ExpiresAt);
    }
}
