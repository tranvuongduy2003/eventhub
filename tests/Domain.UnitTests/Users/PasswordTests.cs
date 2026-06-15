using EventHub.Domain.Exceptions;
using EventHub.Domain.Users;
using FluentAssertions;

namespace EventHub.Domain.UnitTests.Users;

public sealed class PasswordTests
{
    [Fact]
    public void Password_Create_WhenTooShort_Throws()
    {
        var act = () => Password.Create("short1");

        act.Should()
            .Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("PASSWORD_TOO_SHORT");
    }

    [Fact]
    public void Password_Create_WhenMissingLetter_Throws()
    {
        var act = () => Password.Create("12345678!");

        act.Should()
            .Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("PASSWORD_MISSING_LETTER");
    }

    [Fact]
    public void Password_Create_WhenMissingDigit_Throws()
    {
        var act = () => Password.Create("SecurePass!");

        act.Should()
            .Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("PASSWORD_MISSING_DIGIT");
    }

    [Fact]
    public void Password_Create_WhenMissingSpecial_Throws()
    {
        var act = () => Password.Create("SecurePass1");

        act.Should()
            .Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("PASSWORD_MISSING_SPECIAL");
    }

    [Fact]
    public void Password_Create_WhenEmpty_Throws()
    {
        var act = () => Password.Create(string.Empty);

        act.Should()
            .Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("PASSWORD_REQUIRED");
    }

    [Fact]
    public void Password_Create_WhenValid_Succeeds()
    {
        var password = Password.Create("SecurePass1!");

        password.Value.Should().Be("SecurePass1!");
    }

    [Fact]
    public void Password_ForCredentialVerification_WhenEmpty_Throws()
    {
        var act = () => Password.ForCredentialVerification(string.Empty);

        act.Should()
            .Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("PASSWORD_REQUIRED");
    }

    [Fact]
    public void Password_ForCredentialVerification_WhenNonEmpty_SucceedsWithoutStrengthRules()
    {
        var password = Password.ForCredentialVerification("x");

        password.Value.Should().Be("x");
    }
}
