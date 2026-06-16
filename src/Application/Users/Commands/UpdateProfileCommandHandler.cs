using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Abstractions.Storage;
using EventHub.Application.Common;
using EventHub.Domain.Exceptions;
using EventHub.Domain.Users;

namespace EventHub.Application.Users.Commands;

public sealed class UpdateProfileCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository userRepository,
    IObjectStorage objectStorage,
    IPendingDomainEventsCollector pendingDomainEventsCollector,
    IPendingSessionCacheCollector pendingSessionCacheCollector)
    : CommandHandler<UpdateProfileCommand, UpdateProfileResult>
{
    private const string Bucket = "eventhub";

    public override async Task<Result<UpdateProfileResult>> Handle(
        UpdateProfileCommand command,
        CancellationToken cancellationToken)
    {
        if (currentUserAccessor.UserId is not { } userId)
        {
            return Error.Unauthorized("UNAUTHORIZED", "You must be signed in.");
        }

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Error.NotFound("USER_NOT_FOUND", "User not found.");
        }

        if (command.Email is not null)
        {
            var email = EmailAddress.Create(command.Email);
            if (email.Value != user.Email.Value)
            {
                if (await userRepository.ExistsByEmailExcludingUserIdAsync(email.Value, userId, cancellationToken))
                {
                    return ProfileErrors.EmailTaken;
                }
            }
        }

        try
        {
            DisplayName? displayName = command.DisplayName is not null
                ? DisplayName.Create(command.DisplayName)
                : null;

            EmailAddress? email = command.Email is not null
                ? EmailAddress.Create(command.Email)
                : null;

            user.UpdateProfile(displayName, email);

            pendingDomainEventsCollector.AddRange(user.DomainEvents);
            user.ClearDomainEvents();

            pendingSessionCacheCollector.Enqueue(
                new PendingSessionCacheEntry(
                    Guid.Empty,
                    user.Id.Value,
                    DateTimeOffset.MinValue));

            var avatarUrl = user.AvatarImageRef is not null
                ? objectStorage.GetPublicUri(Bucket, user.AvatarImageRef.Value).ToString()
                : null;

            return new UpdateProfileResult(
                user.Id.Value,
                user.DisplayName.Value,
                user.Email.DisplayValue,
                avatarUrl);
        }
        catch (BusinessRuleValidationException exception)
        {
            return Error.Validation(
                exception.Code ?? Error.ValidationFailedCode,
                exception.Message);
        }
    }
}
