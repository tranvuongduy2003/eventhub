using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Common;
using EventHub.Contracts.Events;
using EventHub.Domain.Events;

namespace EventHub.Application.Events.Queries;

public sealed class ListInvitationsQueryHandler(
    IEventInvitationRepository eventInvitationRepository)
    : QueryHandler<ListInvitationsQuery, IReadOnlyList<InvitationResponse>>
{
    public override async Task<Result<IReadOnlyList<InvitationResponse>>> Handle(
        ListInvitationsQuery query,
        CancellationToken cancellationToken)
    {
        var eventId = EventId.From(query.EventId);

        var invitations = await eventInvitationRepository.GetByEventAsync(eventId, cancellationToken);

        var responses = invitations
            .Select(invitation => new InvitationResponse(
                invitation.Id.Value,
                invitation.Email,
                invitation.Status.ToString(),
                invitation.CreatedAt,
                invitation.ExpiresAt,
                invitation.AcceptedAt,
                invitation.RevokedAt))
            .ToList();

        return responses;
    }
}
