using Solution.Domain.Users;
using Solution.Infrastructure.Persistence.Entities;

namespace Solution.Infrastructure.Persistence.Mapping;

internal static class UserPersistenceMapper
{
    public static UserRecord ToUserRecord(User user) =>
        new()
        {
            Id = user.Id.Value,
            Username = user.Username.Value,
            Email = user.Email.Value,
            PasswordHash = user.PasswordHash.Value,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            RowVersion = 1,
        };

    public static User ToUser(UserRecord record) =>
        User.FromPersistence(
            UserId.From(record.Id),
            Username.Create(record.Username),
            EmailAddress.Create(record.Email),
            PasswordHash.Create(record.PasswordHash),
            record.CreatedAt,
            record.UpdatedAt);
}
