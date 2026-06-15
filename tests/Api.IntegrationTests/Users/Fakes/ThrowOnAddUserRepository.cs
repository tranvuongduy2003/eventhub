using EventHub.Application.Abstractions.Persistence;
using EventHub.Domain.Users;

namespace EventHub.Api.IntegrationTests.Users.Fakes;

internal sealed class ThrowOnAddUserRepository : IUserRepository
{
    public Task AddAsync(User user, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Simulated persistence failure.");

    public Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
        Task.FromResult<User?>(null);

    public Task<User?> GetByIdAsync(UserId userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<User?>(null);
}
