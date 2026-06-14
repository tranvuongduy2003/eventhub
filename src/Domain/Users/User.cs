using Solution.Domain.Abstractions;
using Solution.Domain.Events;
using Solution.Domain.Exceptions;
using Solution.Domain.Users;

namespace Solution.Domain.Users;

public sealed class User : AggregateRoot<UserId>
{
    private User()
    {
    }

    public Username Username { get; private set; } = null!;

    public EmailAddress Email { get; private set; } = null!;

    public PasswordHash PasswordHash { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static User Register(
        Username username,
        EmailAddress email,
        Password password,
        PasswordHash passwordHash,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(password);

        var userId = UserId.New();
        var user = new User
        {
            Id = userId,
            Username = username,
            Email = email,
            PasswordHash = passwordHash,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
        };

        user.Raise(new UserRegisteredEvent(userId, username, email));

        return user;
    }

    public static User FromPersistence(
        UserId id,
        Username username,
        EmailAddress email,
        PasswordHash passwordHash,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt) =>
        new()
        {
            Id = id,
            Username = username,
            Email = email,
            PasswordHash = passwordHash,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
        };
}
