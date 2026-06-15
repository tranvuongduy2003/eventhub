using EventHub.Application.Abstractions.Storage;

namespace EventHub.Infrastructure.Storage;

public sealed class NoOpObjectStorage : IObjectStorage
{
    public Task UploadAsync(
        string bucket,
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task DeleteAsync(string bucket, string objectKey, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Uri GetPublicUri(string bucket, string objectKey) =>
        new($"https://localhost/objects/{bucket}/{objectKey}");
}
