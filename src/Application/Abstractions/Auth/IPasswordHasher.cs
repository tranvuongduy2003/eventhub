using EventHub.Domain.Users;

namespace EventHub.Application.Abstractions.Auth;

public interface IPasswordHasher
{
    PasswordHash Hash(Password password);

    bool Verify(Password password, PasswordHash storedHash);
}
