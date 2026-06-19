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

    public AvatarImageRef? AvatarImageRef { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static User Register(
        DisplayName displayName,
        EmailAddress email,
        PasswordHash passwordHash,
        UserRole role,
        DateTimeOffset createdAt)
    {
        var userId = UserId.New();
        var user = new User
        {
            Id = userId,
            DisplayName = displayName,
            Email = email,
            PasswordHash = passwordHash,
            Role = role,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
        };

        user.Raise(new UserRegisteredEvent(userId, displayName, email));

        return user;
    }

    public void UpdateProfile(DisplayName? displayName, EmailAddress? email)
    {
        if (displayName is not null)
        {
            DisplayName = displayName;
        }

        if (email is not null)
        {
            if (email.Value == Email.Value)
            {
                return;
            }

            Email = email;
        }

        UpdatedAt = DateTimeOffset.UtcNow;
        Raise(new UserProfileUpdatedEvent(Id, displayName, email));
    }

    public void SetAvatar(AvatarImageRef avatarImageRef)
    {
        AvatarImageRef = avatarImageRef;
        UpdatedAt = DateTimeOffset.UtcNow;
        Raise(new UserProfileUpdatedEvent(Id, null, null));
    }

    public void RemoveAvatar()
    {
        if (AvatarImageRef is null)
        {
            return;
        }

        AvatarImageRef = null;
        UpdatedAt = DateTimeOffset.UtcNow;
        Raise(new UserProfileUpdatedEvent(Id, null, null));
    }

    public static User FromPersistence(
        UserId id,
        DisplayName displayName,
        EmailAddress email,
        PasswordHash passwordHash,
        UserRole role,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        AvatarImageRef? avatarImageRef = null) =>
        new()
        {
            Id = id,
            DisplayName = displayName,
            Email = email,
            PasswordHash = passwordHash,
            Role = role,
            AvatarImageRef = avatarImageRef,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
        };
}
