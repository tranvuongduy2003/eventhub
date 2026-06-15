using EventHub.Domain.Exceptions;
using EventHub.Domain.Users;
using FluentAssertions;

namespace EventHub.Domain.UnitTests.Users;

public sealed class EmailAddressTests
{
    [Fact]
    public void EmailAddress_Create_WhenInvalidFormat_Throws()
    {
        var act = () => EmailAddress.Create("not-an-email");

        act.Should()
            .Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("EMAIL_INVALID");
    }

    [Fact]
    public void EmailAddress_Create_WhenEmpty_Throws()
    {
        var act = () => EmailAddress.Create("   ");

        act.Should()
            .Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("EMAIL_REQUIRED");
    }

    [Fact]
    public void EmailAddress_Create_WhenTooLong_Throws()
    {
        var localPart = new string('a', 250);
        var act = () => EmailAddress.Create($"{localPart}@example.com");

        act.Should()
            .Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("EMAIL_TOO_LONG");
    }

    [Fact]
    public void EmailAddress_Create_TrimsSurroundingWhitespace()
    {
        var email = EmailAddress.Create("  Jane@Example.com  ");

        email.DisplayValue.Should().Be("Jane@Example.com");
        email.Value.Should().Be("jane@example.com");
    }
}
