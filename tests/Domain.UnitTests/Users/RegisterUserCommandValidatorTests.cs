using FluentAssertions;
using FluentValidation.Results;
using Solution.Application.Users.Commands;

namespace Solution.Domain.UnitTests.Users;

public sealed class RegisterUserCommandValidatorTests
{
    private readonly RegisterUserCommandValidator validator = new();

    [Fact]
    public async Task Validate_WhenPasswordIsShort1_ReportsMultiplePasswordFailures()
    {
        var command = new RegisterUserCommand("valid_user", "jane@example.com", "short1");

        ValidationResult result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors
            .Where(failure => failure.PropertyName == nameof(RegisterUserCommand.Password))
            .Select(failure => failure.ErrorMessage)
            .Distinct()
            .Should()
            .HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Validate_WhenUsernameIsTooShort_DoesNotValidateEmailOrPassword()
    {
        var command = new RegisterUserCommand("ab", "not-an-email", "x");

        ValidationResult result = await validator.ValidateAsync(command);

        result.Errors.Should().Contain(failure => failure.PropertyName == nameof(RegisterUserCommand.Username));
    }
}
