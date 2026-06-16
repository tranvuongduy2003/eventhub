using EventHub.Application.Abstractions.Messaging;

namespace EventHub.Application.Users.Commands;

public sealed record UploadAvatarCommand(
    Stream Content,
    string ContentType,
    string FileName) : ICommand<UploadAvatarResult>;

public sealed record UploadAvatarResult(string AvatarUrl);
