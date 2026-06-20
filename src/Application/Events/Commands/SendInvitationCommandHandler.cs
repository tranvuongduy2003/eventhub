using System.Security.Cryptography;
using System.Text;
using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Common;
using EventHub.Domain.Events;

namespace EventHub.Application.Events.Commands;

public sealed class SendInvitationCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository userRepository,
    IEventUserRoleRepository eventUserRoleRepository,
    IEventInvitationRepository eventInvitationRepository,
    IIntegrationEventPublisher integrationEventPublisher,
    IClock clock)
    : CommandHandler<SendInvitationCommand, SendInvitationResult>
{
    private const int DefaultExpiresInDays = 7;
    private const int MinExpiresInDays = 1;
    private const int MaxExpiresInDays = 30;

    public override async Task<Result<SendInvitationResult>> Handle(
        SendInvitationCommand command,
        CancellationToken cancellationToken)
    {
        var callerId = currentUserAccessor.UserId!.Value;
        var eventId = EventId.From(command.EventId);
        var normalizedEmail = command.Email.Trim().ToLowerInvariant();

        // Self-invite check
        var caller = await userRepository.GetByIdAsync(callerId, cancellationToken);
        if (caller is not null && caller.Email.Value == normalizedEmail)
        {
            return InvitationErrors.SelfInvitationNotAllowed;
        }

        // Check if invitee already has a role on the event
        var invitee = await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (invitee is not null)
        {
            var existingRole = await eventUserRoleRepository.GetByEventAndUserAsync(
                eventId, invitee.Id, cancellationToken);

            if (existingRole is not null)
            {
                return InvitationErrors.RoleAlreadyAssigned;
            }
        }

        // Check for duplicate pending invitation
        var existsPending = await eventInvitationRepository.ExistsPendingByEmailAndEventAsync(
            normalizedEmail, eventId, cancellationToken);

        if (existsPending)
        {
            return InvitationErrors.InvitationAlreadyPending;
        }

        var expiresInDays = command.ExpiresInDays ?? DefaultExpiresInDays;
        expiresInDays = Math.Clamp(expiresInDays, MinExpiresInDays, MaxExpiresInDays);

        var now = clock.UtcNow;
        var expiresAt = now.AddDays(expiresInDays);

        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(tokenBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        var tokenHash = ComputeSha256Hash(token);

        var invitation = EventInvitation.Create(
            eventId,
            normalizedEmail,
            EventRole.Staff,
            callerId,
            tokenHash,
            expiresAt,
            now);

        var invitationId = await eventInvitationRepository.AddAsync(invitation, cancellationToken);

        await integrationEventPublisher.PublishAsync(
            new InvitationCreatedIntegrationEvent(
                InvitationId.From(invitationId),
                eventId,
                normalizedEmail,
                token,
                string.Empty,
                expiresAt),
            cancellationToken);

        return new SendInvitationResult(
            invitationId,
            normalizedEmail,
            invitation.Status.ToString(),
            invitation.CreatedAt,
            invitation.ExpiresAt,
            token);
    }

    private static string ComputeSha256Hash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
