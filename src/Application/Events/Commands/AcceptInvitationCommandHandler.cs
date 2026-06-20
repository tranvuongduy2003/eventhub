using System.Security.Cryptography;
using System.Text;
using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Common;
using EventHub.Domain.Events;
using EventHub.Domain.Users;

namespace EventHub.Application.Events.Commands;

public sealed class AcceptInvitationCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository userRepository,
    IEventUserRoleRepository eventUserRoleRepository,
    IEventInvitationRepository eventInvitationRepository,
    IClock clock)
    : CommandHandler<AcceptInvitationCommand, AcceptInvitationResult>
{
    public override async Task<Result<AcceptInvitationResult>> Handle(
        AcceptInvitationCommand command,
        CancellationToken cancellationToken)
    {
        var callerId = currentUserAccessor.UserId;
        if (callerId is null)
        {
            return Error.Unauthorized("UNAUTHORIZED", "You must be logged in to accept an invitation.");
        }

        var invitationId = InvitationId.From(command.InvitationId);
        var tokenHash = ComputeSha256Hash(command.Token);

        var invitation = await eventInvitationRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (invitation is null || invitation.Id != invitationId)
        {
            return InvitationErrors.InvitationNotFound;
        }

        if (invitation.Status != InvitationStatus.Pending)
        {
            return InvitationErrors.InvitationNotAcceptable;
        }

        if (invitation.ExpiresAt <= clock.UtcNow)
        {
            invitation.MarkExpired();
            await eventInvitationRepository.UpdateAsync(invitation, cancellationToken);
            return InvitationErrors.InvitationNotAcceptable;
        }

        var caller = await userRepository.GetByIdAsync(callerId.Value, cancellationToken);
        if (caller is null || caller.Email.Value != invitation.Email)
        {
            return Error.Forbidden(
                "INVITATION_EMAIL_MISMATCH",
                "This invitation was sent to a different email address.");
        }

        var existingRole = await eventUserRoleRepository.GetByEventAndUserAsync(
            invitation.EventId, callerId.Value, cancellationToken);

        if (existingRole is not null)
        {
            return InvitationErrors.RoleAlreadyAssigned;
        }

        invitation.Accept(clock.UtcNow);
        await eventInvitationRepository.UpdateAsync(invitation, cancellationToken);

        await eventUserRoleRepository.AddAsync(
            EventUserRole.Create(invitation.EventId, callerId.Value, EventRole.Staff, clock.UtcNow),
            cancellationToken);

        return new AcceptInvitationResult(
            invitation.Id.Value,
            invitation.EventId.Value,
            invitation.Email,
            invitation.Status.ToString());
    }

    private static string ComputeSha256Hash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
