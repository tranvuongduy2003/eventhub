using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Common;
using EventHub.Domain.Events;
using EventHub.Domain.Exceptions;
using EventHub.Domain.Users;

namespace EventHub.Application.Events.Commands;

public sealed class AssignRoleCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository userRepository,
    IEventUserRoleRepository eventUserRoleRepository,
    IPermissionAuditEntryRepository auditEntryRepository,
    IClock clock)
    : CommandHandler<AssignRoleCommand, AssignRoleResult>
{
    public override async Task<Result<AssignRoleResult>> Handle(
        AssignRoleCommand command,
        CancellationToken cancellationToken)
    {
        var callerId = currentUserAccessor.UserId!.Value;
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

        if (callerId == targetUserId)
        {
            return Error.Validation(
                "SELF_ASSIGNMENT_NOT_ALLOWED",
                "You cannot assign a role to yourself.");
        }

        var targetUser = await userRepository.GetByIdAsync(targetUserId, cancellationToken);
        if (targetUser is null)
        {
            return Error.NotFound("USER_NOT_FOUND", "The specified user was not found.");
        }

        if (!Enum.TryParse<EventRole>(command.Role, ignoreCase: true, out var requestedRole)
            || (requestedRole != EventRole.Owner && requestedRole != EventRole.Staff))
        {
            return Error.Validation(
                "INVALID_ROLE",
                "Role must be 'Owner' or 'Staff'.");
        }

        var existingAssignment = await eventUserRoleRepository.GetByEventAndUserAsync(
            eventId, targetUserId, cancellationToken);

        if (existingAssignment is not null && existingAssignment.Role == requestedRole)
        {
            return Error.Conflict(
                "ROLE_ALREADY_ASSIGNED",
                $"The user already holds the {requestedRole} role on this event.");
        }

        if (requestedRole == EventRole.Owner)
        {
            var eventRoles = await eventUserRoleRepository.GetByEventAsync(eventId, cancellationToken);
            var currentOwner = eventRoles.FirstOrDefault(assignment => assignment.Role == EventRole.Owner);

            if (currentOwner is not null && currentOwner.UserId != targetUserId)
            {
                await eventUserRoleRepository.UpdateRoleAsync(
                    eventId, currentOwner.UserId, EventRole.Staff, cancellationToken);

                await auditEntryRepository.AddAsync(
                    PermissionAuditEntry.Create(
                        eventId, callerId, currentOwner.UserId,
                        AuditAction.Transferred, EventRole.Owner, EventRole.Staff, clock.UtcNow),
                    cancellationToken);
            }

            if (existingAssignment is not null)
            {
                await eventUserRoleRepository.UpdateRoleAsync(
                    eventId, targetUserId, EventRole.Owner, cancellationToken);
            }
            else
            {
                await eventUserRoleRepository.AddAsync(
                    EventUserRole.Create(eventId, targetUserId, EventRole.Owner, clock.UtcNow),
                    cancellationToken);
            }

            await auditEntryRepository.AddAsync(
                PermissionAuditEntry.Create(
                    eventId, callerId, targetUserId,
                    AuditAction.Assigned, existingAssignment?.Role, EventRole.Owner, clock.UtcNow),
                cancellationToken);
        }
        else
        {
            if (existingAssignment is not null)
            {
                await eventUserRoleRepository.UpdateRoleAsync(
                    eventId, targetUserId, EventRole.Staff, cancellationToken);
            }
            else
            {
                await eventUserRoleRepository.AddAsync(
                    EventUserRole.Create(eventId, targetUserId, EventRole.Staff, clock.UtcNow),
                    cancellationToken);
            }

            await auditEntryRepository.AddAsync(
                PermissionAuditEntry.Create(
                    eventId, callerId, targetUserId,
                    AuditAction.Assigned, existingAssignment?.Role, EventRole.Staff, clock.UtcNow),
                cancellationToken);
        }

        return new AssignRoleResult(
            targetUserId.Value,
            targetUser.DisplayName.Value,
            targetUser.Email.DisplayValue,
            requestedRole.ToString());
    }
}
