namespace EventHub.Application.Options;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string Bucket { get; set; } = "eventhub";
}
