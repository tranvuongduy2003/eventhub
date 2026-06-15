using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Common;
using EventHub.Domain.Exceptions;
using EventHub.Domain.Users;

namespace EventHub.Application.Users.Commands;

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

        if (await userRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken))
        {
            return RegistrationErrors.EmailTaken;
        }

        try
        {
            var displayName = DisplayName.Create(command.DisplayName);
            var email = EmailAddress.Create(command.Email);
            var password = Password.Create(command.Password);
            var passwordHash = passwordHasher.Hash(password);
            var createdAt = clock.UtcNow;

            var user = User.Register(
                displayName,
                email,
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
                user.DisplayName.Value,
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
