using EventHub.Application.Abstractions.Email;

namespace EventHub.Infrastructure.Email;

public sealed class NoOpEmailSender : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
