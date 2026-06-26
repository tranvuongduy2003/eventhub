using EventHub.Application.Abstractions.Storage;
using Minio;
using Minio.DataModel.Args;

namespace EventHub.Infrastructure.Storage;

public sealed class MinioObjectStorage : IObjectStorage, IDisposable
{
    private readonly IMinioClient _client;
    private readonly HashSet<string> _ensuredBuckets = [];

    public MinioObjectStorage(string connectionString)
    {
        var parameters = ParseConnectionString(connectionString);

        var clientBuilder = new MinioClient()
            .WithEndpoint(parameters.Endpoint)
            .WithCredentials(parameters.AccessKey, parameters.SecretKey);

        if (parameters.UseSsl)
        {
            clientBuilder = clientBuilder.WithSSL();
        }

        _client = clientBuilder.Build();
    }

    public async Task UploadAsync(
        string bucket,
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken)
    {
        await EnsureBucketAsync(bucket, cancellationToken);

        var args = new PutObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectKey)
            .WithStreamData(content)
            .WithObjectSize(content.Length)
            .WithContentType(contentType);

        await _client.PutObjectAsync(args, cancellationToken);
    }

    public async Task DeleteAsync(string bucket, string objectKey, CancellationToken cancellationToken)
    {
        var args = new RemoveObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectKey);

        await _client.RemoveObjectAsync(args, cancellationToken);
    }

    public Uri GetPublicUri(string bucket, string objectKey)
    {
        var endpoint = _client.Config?.Endpoint ?? "http://localhost:9000";
        return new Uri($"{endpoint}/{bucket}/{objectKey}");
    }

    public void Dispose() => _client.Dispose();

    private sealed record ConnectionParameters(string Endpoint, string AccessKey, string SecretKey, bool UseSsl);

    private static ConnectionParameters ParseConnectionString(
        string connectionString)
    {
        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        string? endpoint = null;
        var accessKey = "minioadmin";
        var secretKey = "minioadmin";

        foreach (var part in parts)
        {
            var kv = part.Split('=', 2);
            if (kv.Length != 2)
            {
                continue;
            }

            switch (kv[0].Trim().ToLowerInvariant())
            {
                case "endpoint":
                    endpoint = kv[1].Trim();
                    break;
                case "accesskey":
                    accessKey = kv[1].Trim();
                    break;
                case "secretkey":
                    secretKey = kv[1].Trim();
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new InvalidOperationException(
                "MinIO connection string must contain an 'Endpoint' key.");
        }

        var useSsl = endpoint.StartsWith("https", StringComparison.OrdinalIgnoreCase);

        if (endpoint.Contains("://"))
        {
            endpoint = endpoint.Split("://", 2)[1];
        }

        return new ConnectionParameters(endpoint, accessKey, secretKey, useSsl);
    }

    private async Task EnsureBucketAsync(string bucket, CancellationToken cancellationToken)
    {
        if (_ensuredBuckets.Contains(bucket))
        {
            return;
        }

        var existsArgs = new BucketExistsArgs().WithBucket(bucket);
        var exists = await _client.BucketExistsAsync(existsArgs, cancellationToken);

        if (!exists)
        {
            var makeArgs = new MakeBucketArgs().WithBucket(bucket);
            await _client.MakeBucketAsync(makeArgs, cancellationToken);
        }

        _ensuredBuckets.Add(bucket);
    }
}
