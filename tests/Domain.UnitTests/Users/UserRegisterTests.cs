using EventHub.Domain.Events;
using EventHub.Domain.Exceptions;
using EventHub.Domain.Users;
using FluentAssertions;

namespace EventHub.Domain.UnitTests.Users;

public class UserRegisterTests
{
    private static readonly DateTimeOffset RegisteredAt = new(2026, 5, 23, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Register_WithValidInput_CreatesUser()
    {
        var user = RegisterValidUser();

        user.Id.Value.Should().NotBe(Guid.Empty);
        user.DisplayName.Value.Should().Be("Jane Organizer");
        user.Email.Value.Should().Be("jane@example.com");
        user.Role.Should().Be(UserRole.Organizer);
    }

    [Fact]
    public void Register_WithInvalidDisplayName_Throws()
    {
        var act = () => DisplayName.Create("   ");

        act.Should()
            .Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("DISPLAY_NAME_REQUIRED");
    }

    [Fact]
    public void Register_RaisesUserRegisteredEvent()
    {
        var user = RegisterValidUser();

        user.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserRegisteredEvent>()
            .Which.Should().BeEquivalentTo(
                new UserRegisteredEvent(user.Id, user.DisplayName, user.Email),
                options => options.Excluding(domainEvent => domainEvent.OccurredOn));
    }

    [Fact]
    public void Register_WithAttendeeRole_CreatesUserWithAttendeeRole()
    {
        var user = RegisterAttendeeUser();

        user.Id.Value.Should().NotBe(Guid.Empty);
        user.DisplayName.Value.Should().Be("Jane Attendee");
        user.Email.Value.Should().Be("jane.attendee@example.com");
        user.Role.Should().Be(UserRole.Attendee);
    }

    [Fact]
    public void Register_WithAttendeeRole_RaisesUserRegisteredEvent()
    {
        var user = RegisterAttendeeUser();

        user.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserRegisteredEvent>()
            .Which.Should().BeEquivalentTo(
                new UserRegisteredEvent(user.Id, user.DisplayName, user.Email),
                options => options.Excluding(domainEvent => domainEvent.OccurredOn));
    }

    [Fact]
    public void Register_WithAttendeeRole_SetsCorrectIdAndTimestamps()
    {
        var user = RegisterAttendeeUser();

        user.Id.Value.Should().NotBe(Guid.Empty);
        user.CreatedAt.Should().Be(RegisteredAt);
        user.UpdatedAt.Should().Be(RegisteredAt);
    }

    private static User RegisterValidUser() =>
        User.Register(
            DisplayName.Create("Jane Organizer"),
            EmailAddress.Create("Jane@Example.com"),
            PasswordHash.Create("hashed-password-stub"),
            UserRole.Organizer,
            RegisteredAt);

    private static User RegisterAttendeeUser() =>
        User.Register(
            DisplayName.Create("Jane Attendee"),
            EmailAddress.Create("jane.attendee@example.com"),
            PasswordHash.Create("hashed-password-stub"),
            UserRole.Attendee,
            RegisteredAt);
}
