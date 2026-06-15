using EventHub.Domain.Users;

namespace EventHub.Infrastructure.Persistence.Entities;

public sealed class UserRecord
{
    public Guid Id { get; set; }

    public required string DisplayName { get; set; }

    public required string Email { get; set; }

    public required string PasswordHash { get; set; }

    public UserRole Role { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public long RowVersion { get; set; }

    public ICollection<UserSessionRecord> Sessions { get; set; } = [];
}
