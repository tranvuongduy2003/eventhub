using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Abstractions.Storage;
using EventHub.Application.Common;
using EventHub.Domain.Users;

namespace EventHub.Application.Users.Commands;

public sealed class RemoveAvatarCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository userRepository,
    IObjectStorage objectStorage,
    IPendingDomainEventsCollector pendingDomainEventsCollector,
    IPendingSessionCacheCollector pendingSessionCacheCollector)
    : CommandHandler<RemoveAvatarCommand>
{
    private const string Bucket = "eventhub";

    public override async Task<Result> Handle(
        RemoveAvatarCommand command,
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

        if (user.AvatarImageRef is null)
        {
            return Result.Success();
        }

        await objectStorage.DeleteAsync(Bucket, user.AvatarImageRef.Value, cancellationToken);

        user.RemoveAvatar();

        pendingDomainEventsCollector.AddRange(user.DomainEvents);
        user.ClearDomainEvents();

        pendingSessionCacheCollector.Enqueue(
            new PendingSessionCacheEntry(
                Guid.Empty,
                user.Id.Value,
                DateTimeOffset.MinValue));

        return Result.Success();
    }
}
