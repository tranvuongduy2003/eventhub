using Solution.Domain.Users;

namespace Solution.Application.Abstractions.Auth;

public interface IPasswordHasher
{
    PasswordHash Hash(Password password);

    bool Verify(Password password, PasswordHash storedHash);
}
