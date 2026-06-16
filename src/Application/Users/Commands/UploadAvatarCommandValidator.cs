using FluentValidation;

namespace EventHub.Application.Users.Commands;

public sealed class UploadAvatarCommandValidator : AbstractValidator<UploadAvatarCommand>
{
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];

    public UploadAvatarCommandValidator()
    {
        RuleFor(command => command.ContentType)
            .Must(contentType => AllowedContentTypes.Contains(contentType.ToLowerInvariant()))
            .WithMessage("Only JPEG, PNG, and WebP images are supported.");

        RuleFor(command => command.Content.Length)
            .LessThanOrEqualTo(5 * 1024 * 1024)
            .WithMessage("File size must not exceed 5 MB.");
    }
}
