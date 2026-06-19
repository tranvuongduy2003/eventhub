using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Common;
using EventHub.Contracts.Events;
using EventHub.Domain.Events;

namespace EventHub.Application.Events.Queries;

public sealed class ListEventRoleAssignmentsQueryHandler(
    ICurrentUserAccessor currentUserAccessor,
    IEventUserRoleRepository eventUserRoleRepository,
    IUserRepository userRepository)
    : QueryHandler<ListEventRoleAssignmentsQuery, IReadOnlyList<EventRoleAssignmentResponse>>
{
    public override async Task<Result<IReadOnlyList<EventRoleAssignmentResponse>>> Handle(
        ListEventRoleAssignmentsQuery query,
        CancellationToken cancellationToken)
    {
        if (currentUserAccessor.UserId is not { } callerId)
        {
            return Error.Unauthorized("UNAUTHORIZED", "You must be logged in.");
        }

        var eventId = EventId.From(query.EventId);

        var callerRole = await eventUserRoleRepository.GetByEventAndUserAsync(
            eventId, callerId, cancellationToken);

        if (callerRole is null || callerRole.Role != EventRole.Owner)
        {
            return Error.Forbidden(
                "INSUFFICIENT_PERMISSIONS",
                "Only the event owner can view role assignments.");
        }

        var assignments = await eventUserRoleRepository.GetByEventAsync(eventId, cancellationToken);

        var responses = new List<EventRoleAssignmentResponse>(assignments.Count);

        foreach (var assignment in assignments)
        {
            var user = await userRepository.GetByIdAsync(assignment.UserId, cancellationToken);
            if (user is not null)
            {
                responses.Add(new EventRoleAssignmentResponse(
                    assignment.UserId.Value,
                    user.DisplayName.Value,
                    user.Email.DisplayValue,
                    assignment.Role.ToString()));
            }
        }

        return responses;
    }
}
