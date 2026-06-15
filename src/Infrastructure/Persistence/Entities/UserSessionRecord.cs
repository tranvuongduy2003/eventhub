namespace EventHub.Infrastructure.Persistence.Entities;

public sealed class UserSessionRecord
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset LastSeenAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public UserRecord? User { get; set; }
}
