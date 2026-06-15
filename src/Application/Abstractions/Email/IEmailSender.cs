namespace EventHub.Application.Abstractions.Email;

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}

public sealed record EmailMessage(string Recipient, string Subject, string HtmlBody);
