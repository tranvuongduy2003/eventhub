using EventHub.Application.Abstractions.Auth;
using EventHub.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace EventHub.Infrastructure.Auth;

internal sealed class IdentityPasswordHasher : IPasswordHasher
{
    private readonly Microsoft.AspNetCore.Identity.PasswordHasher<ApplicationIdentityUser> _passwordHasher = new();

    public PasswordHash Hash(Password password) =>
        PasswordHash.Create(_passwordHasher.HashPassword(new ApplicationIdentityUser(), password.Value));

    public bool Verify(Password password, PasswordHash storedHash)
    {
        var verificationResult = _passwordHasher.VerifyHashedPassword(
            new ApplicationIdentityUser(),
            storedHash.Value,
            password.Value);

        return verificationResult is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
