using EventHub.Domain.Abstractions;
using EventHub.Domain.Events;

namespace EventHub.Domain.Users;

public sealed class User : AggregateRoot<UserId>
{
    private User()
    {
    }

    public DisplayName DisplayName { get; private set; } = null!;

    public EmailAddress Email { get; private set; } = null!;

    public PasswordHash PasswordHash { get; private set; } = null!;

    public UserRole Role { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static User Register(
        DisplayName displayName,
        EmailAddress email,
        PasswordHash passwordHash,
        DateTimeOffset createdAt)
    {
        var userId = UserId.New();
        var user = new User
        {
            Id = userId,
            DisplayName = displayName,
            Email = email,
            PasswordHash = passwordHash,
            Role = UserRole.Organizer,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
        };

        user.Raise(new UserRegisteredEvent(userId, displayName, email));

        return user;
    }

    public static User FromPersistence(
        UserId id,
        DisplayName displayName,
        EmailAddress email,
        PasswordHash passwordHash,
        UserRole role,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt) =>
        new()
        {
            Id = id,
            DisplayName = displayName,
            Email = email,
            PasswordHash = passwordHash,
            Role = role,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
        };
}
