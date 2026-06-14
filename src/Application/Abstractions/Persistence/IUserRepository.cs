using Solution.Domain.Users;

namespace Solution.Application.Abstractions.Persistence;

public interface IUserRepository
{
    Task AddAsync(User user, CancellationToken cancellationToken = default);

    Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(UserId userId, CancellationToken cancellationToken = default);
}
