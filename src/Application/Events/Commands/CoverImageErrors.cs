using EventHub.Application.Common;

namespace EventHub.Application.Events.Commands;

public static class CoverImageErrors
{
    public const string InvalidFileTypeCode = "COVER_IMAGE_FORMAT_UNSUPPORTED";
    public const string FileTooLargeCode = "COVER_IMAGE_TOO_LARGE";
    public const string EventNotFoundCode = "EVENT_NOT_FOUND";
    public const string EventStatusNotAllowedCode = "EVENT_STATUS_NOT_ALLOWED";

    public static readonly Error InvalidFileType = Error.Validation(
        InvalidFileTypeCode,
        "Only JPEG, PNG, and WebP images are supported.");

    public static readonly Error FileTooLarge = Error.Validation(
        FileTooLargeCode,
        "File size must not exceed 5 MB.");

    public static readonly Error EventNotFound = Error.NotFound(
        EventNotFoundCode,
        "Event not found.");

    public static readonly Error EventStatusNotAllowed = Error.Validation(
        EventStatusNotAllowedCode,
        "Cover images can only be set on Draft or Published events.");
}
