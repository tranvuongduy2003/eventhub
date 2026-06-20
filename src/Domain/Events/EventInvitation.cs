using EventHub.Domain.Abstractions;
using EventHub.Domain.Exceptions;
using EventHub.Domain.Users;

namespace EventHub.Domain.Events;

public sealed class EventInvitation : AggregateRoot<InvitationId>
{
    private EventInvitation() { }

    public EventId EventId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public EventRole Role { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public InvitationStatus Status { get; private set; }
    public UserId InviterId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? AcceptedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    public static EventInvitation Create(
        EventId eventId,
        string email,
        EventRole role,
        UserId inviterId,
        string tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset createdAt)
    {
        if (role != EventRole.Staff)
        {
            throw new BusinessRuleValidationException(
                "INVITATION_ONLY_STAFF",
                "Only the Staff role can be assigned via invitation.");
        }

        if (expiresAt <= createdAt)
        {
            throw new BusinessRuleValidationException(
                "INVITATION_INVALID_EXPIRY",
                "Expiry date must be in the future.");
        }

        return new EventInvitation
        {
            EventId = eventId,
            Email = email,
            Role = role,
            TokenHash = tokenHash,
            Status = InvitationStatus.Pending,
            InviterId = inviterId,
            CreatedAt = createdAt,
            ExpiresAt = expiresAt,
        };
    }

    public void Accept(DateTimeOffset acceptedAt)
    {
        if (Status != InvitationStatus.Pending)
        {
            throw new BusinessRuleValidationException(
                "INVITATION_NOT_ACCEPTABLE",
                $"Cannot accept an invitation with status '{Status}'.");
        }

        Status = InvitationStatus.Accepted;
        AcceptedAt = acceptedAt;
    }

    public void Revoke(DateTimeOffset revokedAt)
    {
        if (Status != InvitationStatus.Pending)
        {
            throw new BusinessRuleValidationException(
                "INVITATION_NOT_ACCEPTABLE",
                $"Cannot revoke an invitation with status '{Status}'.");
        }

        Status = InvitationStatus.Revoked;
        RevokedAt = revokedAt;
    }

    public void MarkExpired()
    {
        if (Status != InvitationStatus.Pending)
        {
            throw new BusinessRuleValidationException(
                "INVITATION_NOT_ACCEPTABLE",
                $"Cannot expire an invitation with status '{Status}'.");
        }

        Status = InvitationStatus.Expired;
    }

    public static EventInvitation FromPersistence(
        InvitationId id,
        EventId eventId,
        string email,
        EventRole role,
        string tokenHash,
        InvitationStatus status,
        UserId inviterId,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        DateTimeOffset? acceptedAt,
        DateTimeOffset? revokedAt) =>
        new()
        {
            Id = id,
            EventId = eventId,
            Email = email,
            Role = role,
            TokenHash = tokenHash,
            Status = status,
            InviterId = inviterId,
            CreatedAt = createdAt,
            ExpiresAt = expiresAt,
            AcceptedAt = acceptedAt,
            RevokedAt = revokedAt,
        };
}
