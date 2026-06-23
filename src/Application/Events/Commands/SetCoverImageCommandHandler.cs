using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Abstractions.Storage;
using EventHub.Application.Common;
using EventHub.Domain.Events;
using EventHub.Domain.Exceptions;

namespace EventHub.Application.Events.Commands;

public sealed class SetCoverImageCommandHandler(
    IEventRepository eventRepository,
    IObjectStorage objectStorage,
    IPendingDomainEventsCollector pendingDomainEventsCollector)
    : CommandHandler<SetCoverImageCommand, SetCoverImageResult>
{
    private const string Bucket = "eventhub";
    private const long MaxFileSize = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];

    public override async Task<Result<SetCoverImageResult>> Handle(
        SetCoverImageCommand command,
        CancellationToken cancellationToken)
    {
        var eventId = EventId.From(command.EventId);

        var eventAggregate = await eventRepository.GetByIdAsync(eventId, cancellationToken);
        if (eventAggregate is null)
        {
            return CoverImageErrors.EventNotFound;
        }

        if (eventAggregate.Status != EventStatus.Draft && eventAggregate.Status != EventStatus.Published)
        {
            return CoverImageErrors.EventStatusNotAllowed;
        }

        if (!AllowedContentTypes.Contains(command.ContentType.ToLowerInvariant()))
        {
            return CoverImageErrors.InvalidFileType;
        }

        if (command.Content.Length > MaxFileSize)
        {
            return CoverImageErrors.FileTooLarge;
        }

        if (eventAggregate.CoverImageRef is not null)
        {
            await objectStorage.DeleteAsync(Bucket, eventAggregate.CoverImageRef.Value, cancellationToken);
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

        var objectKey = $"events/{eventId}/cover/{Guid.NewGuid()}{extension}";

        await objectStorage.UploadAsync(Bucket, objectKey, command.Content, command.ContentType, cancellationToken);

        try
        {
            var coverImageRef = CoverImageRef.Create(objectKey);
            eventAggregate.SetCoverImage(coverImageRef);
        }
        catch (BusinessRuleValidationException exception)
        {
            return Error.Validation(
                exception.Code ?? Error.ValidationFailedCode,
                exception.Message);
        }

        await eventRepository.Update(eventAggregate, cancellationToken);

        pendingDomainEventsCollector.AddRange(eventAggregate.DomainEvents);
        eventAggregate.ClearDomainEvents();

        var coverImageUrl = objectStorage.GetPublicUri(Bucket, objectKey).ToString();

        return new SetCoverImageResult(coverImageUrl);
    }
}
