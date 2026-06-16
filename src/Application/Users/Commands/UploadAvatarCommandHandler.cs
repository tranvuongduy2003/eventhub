using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Abstractions.Storage;
using EventHub.Application.Common;
using EventHub.Domain.Users;

namespace EventHub.Application.Users.Commands;

public sealed class UploadAvatarCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository userRepository,
    IObjectStorage objectStorage,
    IPendingDomainEventsCollector pendingDomainEventsCollector,
    IPendingSessionCacheCollector pendingSessionCacheCollector)
    : CommandHandler<UploadAvatarCommand, UploadAvatarResult>
{
    private const string Bucket = "eventhub";
    private const long MaxFileSize = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];

    public override async Task<Result<UploadAvatarResult>> Handle(
        UploadAvatarCommand command,
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

        if (!AllowedContentTypes.Contains(command.ContentType.ToLowerInvariant()))
        {
            return ProfileErrors.InvalidFileType;
        }

        if (command.Content.Length > MaxFileSize)
        {
            return ProfileErrors.FileTooLarge;
        }

        if (user.AvatarImageRef is not null)
        {
            await objectStorage.DeleteAsync(Bucket, user.AvatarImageRef.Value, cancellationToken);
        }

        var extension = Path.GetExtension(command.FileName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = command.ContentType.ToLowerInvariant() switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => ".bin"
            };
        }

        var objectKey = $"avatars/{userId}/{Guid.NewGuid()}{extension}";

        await objectStorage.UploadAsync(Bucket, objectKey, command.Content, command.ContentType, cancellationToken);

        var avatarRef = AvatarImageRef.Create(objectKey);
        user.SetAvatar(avatarRef);

        pendingDomainEventsCollector.AddRange(user.DomainEvents);
        user.ClearDomainEvents();

        pendingSessionCacheCollector.Enqueue(
            new PendingSessionCacheEntry(
                Guid.Empty,
                user.Id.Value,
                DateTimeOffset.MinValue));

        var avatarUrl = objectStorage.GetPublicUri(Bucket, objectKey).ToString();

        return new UploadAvatarResult(avatarUrl);
    }
}
