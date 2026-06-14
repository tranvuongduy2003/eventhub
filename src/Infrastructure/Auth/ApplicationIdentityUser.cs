namespace Solution.Infrastructure.Auth;

internal sealed class ApplicationIdentityUser
{
    public Guid Id { get; init; } = Guid.NewGuid();
}
