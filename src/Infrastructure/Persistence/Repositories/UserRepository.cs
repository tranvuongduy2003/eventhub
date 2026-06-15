using EventHub.Application.Abstractions.Persistence;
using EventHub.Domain.Users;
using EventHub.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(ApplicationDatabaseContext databaseContext) : IUserRepository
{
    public async Task AddAsync(User user, CancellationToken cancellationToken = default) =>
        await databaseContext.Users.AddAsync(UserPersistenceMapper.ToUserRecord(user), cancellationToken);

    public Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
        databaseContext.Users.AsNoTracking().AnyAsync(user => user.Email == normalizedEmail, cancellationToken);

    public async Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        var record = await databaseContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Email == normalizedEmail, cancellationToken);

        return record is null ? null : UserPersistenceMapper.ToUser(record);
    }

    public async Task<User?> GetByIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var record = await databaseContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == userId.Value, cancellationToken);

        return record is null ? null : UserPersistenceMapper.ToUser(record);
    }
}
