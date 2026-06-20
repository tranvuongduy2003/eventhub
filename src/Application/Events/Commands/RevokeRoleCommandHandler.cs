using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Common;
using EventHub.Domain.Events;
using EventHub.Domain.Exceptions;
using EventHub.Domain.Users;

namespace EventHub.Application.Events.Commands;

public sealed class RevokeRoleCommandHandler(
    IEventUserRoleRepository eventUserRoleRepository)
    : CommandHandler<RevokeRoleCommand>
{
    public override async Task<Result> Handle(
        RevokeRoleCommand command,
        CancellationToken cancellationToken)
    {
        var eventId = EventId.From(command.EventId);

        UserId targetUserId;
        try
        {
            targetUserId = UserId.From(command.UserId);
        }
        catch (BusinessRuleValidationException)
        {
            return Error.Validation("USER_ID_INVALID", "User id cannot be empty.");
        }

        var targetAssignment = await eventUserRoleRepository.GetByEventAndUserAsync(
            eventId, targetUserId, cancellationToken);

        if (targetAssignment is null)
        {
            return Result.Success();
        }

        if (targetAssignment.Role == EventRole.Owner)
        {
            return Error.Validation(
                "CANNOT_REVOKE_OWNER",
                "Cannot revoke the owner role. Transfer ownership to another user first.");
        }

        await eventUserRoleRepository.DeleteByEventAndUserAsync(
            eventId, targetUserId, cancellationToken);

        return Result.Success();
    }
}
