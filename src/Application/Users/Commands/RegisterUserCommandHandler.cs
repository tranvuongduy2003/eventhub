using Solution.Application.Abstractions.Auth;
using Solution.Application.Abstractions.Messaging;
using Solution.Application.Abstractions.Persistence;
using Solution.Application.Abstractions.Services;
using Solution.Application.Common;
using Solution.Domain.Exceptions;
using Solution.Domain.Users;

namespace Solution.Application.Users.Commands;

public sealed class RegisterUserCommandHandler(
    IUserRepository userRepository,
    ISessionStore sessionStore,
    IPasswordHasher passwordHasher,
    IClock clock,
    IPendingDomainEventsCollector pendingDomainEventsCollector,
    IPendingSessionCacheCollector pendingSessionCacheCollector)
    : CommandHandler<RegisterUserCommand, RegisterUserResult>
{
    public override async Task<Result<RegisterUserResult>> Handle(
        RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = EmailAddress.Create(command.Email).Value;

        if (await userRepository.ExistsByUsernameAsync(command.Username, cancellationToken))
        {
            return RegistrationErrors.UsernameTaken;
        }

        if (await userRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken))
        {
            return RegistrationErrors.EmailTaken;
        }

        try
        {
            var username = Username.Create(command.Username);
            var email = EmailAddress.Create(command.Email);
            var password = Password.Create(command.Password);
            var passwordHash = passwordHasher.Hash(password);
            var createdAt = clock.UtcNow;

            var user = User.Register(
                username,
                email,
                password,
                passwordHash,
                createdAt);

            await userRepository.AddAsync(user, cancellationToken);

            var session = await sessionStore.CreateSessionAsync(user.Id, cancellationToken);
            pendingSessionCacheCollector.Enqueue(
                new PendingSessionCacheEntry(
                    session.SessionId,
                    user.Id.Value,
                    session.ExpiresAt));

            pendingDomainEventsCollector.AddRange(user.DomainEvents);
            user.ClearDomainEvents();

            return new RegisterUserResult(
                user.Id.Value,
                user.Username.Value,
                email.DisplayValue,
                user.CreatedAt,
                session.SessionId,
                session.ExpiresAt);
        }
        catch (BusinessRuleValidationException exception)
        {
            return Error.Validation(
                exception.Code ?? Error.ValidationFailedCode,
                exception.Message);
        }
    }
}
