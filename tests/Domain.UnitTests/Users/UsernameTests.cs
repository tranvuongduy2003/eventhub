using FluentAssertions;
using Solution.Domain.Exceptions;
using Solution.Domain.Users;

namespace Solution.Domain.UnitTests.Users;

public sealed class UsernameTests
{
    [Fact]
    public void Username_Create_WhenTooShort_Throws()
    {
        var act = () => Username.Create("ab");

        act.Should()
            .Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("USERNAME_LENGTH");
    }

    [Fact]
    public void Username_Create_WhenTooLong_Throws()
    {
        var act = () => Username.Create(new string('a', 33));

        act.Should()
            .Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("USERNAME_LENGTH");
    }

    [Fact]
    public void Username_Create_WhenEmpty_Throws()
    {
        var act = () => Username.Create("   ");

        act.Should()
            .Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("USERNAME_REQUIRED");
    }

    [Fact]
    public void Username_Create_WhenInvalidCharacters_Throws()
    {
        var act = () => Username.Create("user name");

        act.Should()
            .Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("USERNAME_INVALID_CHARACTERS");
    }

    [Fact]
    public void Username_Create_WhenValid_ReturnsValue()
    {
        var username = Username.Create("trader_jane");

        username.Value.Should().Be("trader_jane");
    }
}
