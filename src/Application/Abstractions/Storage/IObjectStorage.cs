namespace EventHub.Application.Abstractions.Storage;

public interface IObjectStorage
{
    Task UploadAsync(
        string bucket,
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken);

    Task DeleteAsync(string bucket, string objectKey, CancellationToken cancellationToken);

    Uri GetPublicUri(string bucket, string objectKey);
}
