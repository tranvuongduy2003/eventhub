using Microsoft.EntityFrameworkCore;
using Solution.Application.Abstractions.Persistence;
using Solution.Domain.Users;
using Solution.Infrastructure.Persistence;

namespace Solution.Api.IntegrationTests.Users.Fakes;

internal sealed class ThrowOnAddUserRepository(ApplicationDatabaseContext databaseContext) : IUserRepository
{
    public Task AddAsync(User user, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Simulated persistence failure.");

    public Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
        databaseContext.Users.AsNoTracking().AnyAsync(user => user.Username == username, cancellationToken);

    public Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
        databaseContext.Users.AsNoTracking().AnyAsync(user => user.Email == normalizedEmail, cancellationToken);

    public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
        Task.FromResult<User?>(null);

    public Task<User?> GetByIdAsync(UserId userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<User?>(null);
}
