using EventHub.Application.Common;

namespace EventHub.Application.Events;

public static class InvitationErrors
{
    public static readonly Error InvitationAlreadyPending = Error.Conflict(
        "INVITATION_ALREADY_PENDING",
        "An invitation for this email is already pending for this event.");

    public static readonly Error InvitationNotFound = Error.NotFound(
        "INVITATION_NOT_FOUND",
        "The invitation was not found.");

    public static readonly Error InvitationNotAcceptable = Error.Validation(
        "INVITATION_NOT_ACCEPTABLE",
        "The invitation cannot be accepted because it is not in a pending state.");

    public static readonly Error RoleAlreadyAssigned = Error.Conflict(
        "ROLE_ALREADY_ASSIGNED",
        "The invitee already has a role on this event.");

    public static readonly Error SelfInvitationNotAllowed = Error.Validation(
        "SELF_INVITATION_NOT_ALLOWED",
        "You cannot invite yourself to an event.");
}
