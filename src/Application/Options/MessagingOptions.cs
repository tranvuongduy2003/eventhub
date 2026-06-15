namespace EventHub.Application.Options;

public sealed class MessagingOptions
{
    public const string SectionName = "Messaging";

    public string ExchangeName { get; set; } = "eventhub.integration";
}
