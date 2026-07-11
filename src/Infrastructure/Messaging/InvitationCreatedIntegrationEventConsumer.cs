using EventHub.Application.Abstractions.Email;
using EventHub.Domain.Events;

namespace EventHub.Infrastructure.Messaging;

public sealed class InvitationCreatedIntegrationEventConsumer(IEmailSender emailSender)
    : IntegrationEventConsumer<InvitationCreatedIntegrationEvent>
{
    public override Task ConsumeAsync(
        InvitationCreatedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken) =>
        emailSender.SendAsync(CreateInvitationEmail(integrationEvent), cancellationToken);

    private static EmailMessage CreateInvitationEmail(InvitationCreatedIntegrationEvent integrationEvent)
    {
        var eventTitle = string.IsNullOrWhiteSpace(integrationEvent.EventTitle)
            ? "an EventHub event"
            : integrationEvent.EventTitle;

        var htmlBody =
            $"""
            <p>You have been invited to help manage {eventTitle}.</p>
            <p>Use this invitation token to accept: {integrationEvent.Token}</p>
            <p>This invitation expires at {integrationEvent.ExpiresAt:O}.</p>
            """;

        return new EmailMessage(
            integrationEvent.Email,
            "You're invited to EventHub",
            htmlBody);
    }
}
