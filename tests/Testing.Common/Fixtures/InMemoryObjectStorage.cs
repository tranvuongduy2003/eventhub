using EventHub.Application.Abstractions.Storage;

namespace EventHub.Testing.Common.Fixtures;

public sealed class InMemoryObjectStorage : IObjectStorage
{
    private readonly Dictionary<string, Dictionary<string, (byte[] Content, string ContentType)>> _buckets = new();

    public Task UploadAsync(
        string bucket,
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken)
    {
        if (!_buckets.TryGetValue(bucket, out var bucketObjects))
        {
            bucketObjects = new Dictionary<string, (byte[], string)>();
            _buckets[bucket] = bucketObjects;
        }

        using var memoryStream = new MemoryStream();
        content.CopyTo(memoryStream);
        bucketObjects[objectKey] = (memoryStream.ToArray(), contentType);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string bucket, string objectKey, CancellationToken cancellationToken)
    {
        if (_buckets.TryGetValue(bucket, out var bucketObjects))
        {
            bucketObjects.Remove(objectKey);
        }

        return Task.CompletedTask;
    }

    public Uri GetPublicUri(string bucket, string objectKey) =>
        new($"https://localhost/objects/{bucket}/{objectKey}");

    public bool Exists(string bucket, string objectKey) =>
        _buckets.TryGetValue(bucket, out var bucketObjects) && bucketObjects.ContainsKey(objectKey);
}
