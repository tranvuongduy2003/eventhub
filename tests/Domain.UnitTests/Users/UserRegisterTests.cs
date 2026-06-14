using FluentAssertions;
using Solution.Domain.Events;
using Solution.Domain.Exceptions;
using Solution.Domain.Users;

namespace Solution.Domain.UnitTests.Users;

public class UserRegisterTests
{
    private static readonly DateTimeOffset RegisteredAt = new(2026, 5, 23, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Register_WithValidInput_CreatesUser()
    {
        var user = RegisterValidUser();

        user.Id.Value.Should().NotBe(Guid.Empty);
        user.Username.Value.Should().Be("trader_jane");
        user.Email.Value.Should().Be("jane@example.com");
    }

    [Fact]
    public void Register_WithInvalidUsername_Throws()
    {
        var act = () => Username.Create("ab");

        act.Should()
            .Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("USERNAME_LENGTH");
    }

    [Fact]
    public void Register_RaisesUserRegisteredEvent()
    {
        var user = RegisterValidUser();

        user.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserRegisteredEvent>()
            .Which.Should().BeEquivalentTo(
                new UserRegisteredEvent(user.Id, user.Username, user.Email),
                options => options.Excluding(domainEvent => domainEvent.OccurredOn));
    }

    private static User RegisterValidUser() =>
        User.Register(
            Username.Create("trader_jane"),
            EmailAddress.Create("Jane@Example.com"),
            Password.Create("SecurePass1!"),
            PasswordHash.Create("hashed-password-stub"),
            RegisteredAt);
}
