using EventHub.Domain.Users;
using EventHub.Infrastructure.Persistence.Entities;

namespace EventHub.Infrastructure.Persistence.Mapping;

internal static class UserPersistenceMapper
{
    public static UserRecord ToUserRecord(User user) =>
        new()
        {
            Id = user.Id.Value,
            DisplayName = user.DisplayName.Value,
            Email = user.Email.Value,
            PasswordHash = user.PasswordHash.Value,
            Role = user.Role,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            RowVersion = 1,
        };

    public static User ToUser(UserRecord record) =>
        User.FromPersistence(
            UserId.From(record.Id),
            DisplayName.Create(record.DisplayName),
            EmailAddress.Create(record.Email),
            PasswordHash.Create(record.PasswordHash),
            record.Role,
            record.CreatedAt,
            record.UpdatedAt);
}
