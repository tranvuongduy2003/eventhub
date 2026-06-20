using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Common;
using EventHub.Domain.Events;

namespace EventHub.Application.Events.Commands;

public sealed class RevokeInvitationCommandHandler(
    IEventInvitationRepository eventInvitationRepository,
    IClock clock)
    : CommandHandler<RevokeInvitationCommand>
{
    public override async Task<Result> Handle(
        RevokeInvitationCommand command,
        CancellationToken cancellationToken)
    {
        var invitationId = InvitationId.From(command.InvitationId);
        var eventId = EventId.From(command.EventId);

        var invitation = await eventInvitationRepository.GetByIdAsync(invitationId, cancellationToken);
        if (invitation is null)
        {
            return InvitationErrors.InvitationNotFound;
        }

        if (invitation.EventId != eventId)
        {
            return InvitationErrors.InvitationNotFound;
        }

        if (invitation.Status != InvitationStatus.Pending)
        {
            return InvitationErrors.InvitationNotAcceptable;
        }

        invitation.Revoke(clock.UtcNow);

        await eventInvitationRepository.UpdateAsync(invitation, cancellationToken);

        return Result.Success();
    }
}
