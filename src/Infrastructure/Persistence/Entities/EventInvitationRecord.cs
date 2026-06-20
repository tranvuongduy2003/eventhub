using EventHub.Domain.Events;

namespace EventHub.Infrastructure.Persistence.Entities;

public sealed class EventInvitationRecord
{
    public int Id { get; set; }

    public int EventId { get; set; }

    public string Email { get; set; } = string.Empty;

    public EventRole Role { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public InvitationStatus Status { get; set; }

    public Guid InviterId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? AcceptedAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public UserRecord? Inviter { get; set; }
}
