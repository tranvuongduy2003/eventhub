using EventHub.Application.Abstractions.Email;
using Microsoft.Extensions.Logging;

namespace EventHub.Infrastructure.Email;

public sealed class LocalEmailSender(ILogger<LocalEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        logger.LogInformation(
            "Local email queued for {Recipient} with subject {Subject}",
            message.Recipient,
            message.Subject);

        return Task.CompletedTask;
    }
}
