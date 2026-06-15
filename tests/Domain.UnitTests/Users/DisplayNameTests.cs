using EventHub.Domain.Exceptions;
using EventHub.Domain.Users;
using FluentAssertions;

namespace EventHub.Domain.UnitTests.Users;

public sealed class DisplayNameTests
{
    [Fact]
    public void DisplayName_Create_WhenEmpty_Throws()
    {
        var act = () => DisplayName.Create("   ");

        act.Should()
            .Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("DISPLAY_NAME_REQUIRED");
    }

    [Fact]
    public void DisplayName_Create_WhenTooLong_Throws()
    {
        var act = () => DisplayName.Create(new string('a', 65));

        act.Should()
            .Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("DISPLAY_NAME_LENGTH");
    }

    [Fact]
    public void DisplayName_Create_WhenValid_ReturnsTrimmedValue()
    {
        var displayName = DisplayName.Create("  Jane Organizer  ");

        displayName.Value.Should().Be("Jane Organizer");
    }

    [Fact]
    public void DisplayName_Create_WhenUnicodeAllowed_ReturnsValue()
    {
        var displayName = DisplayName.Create("Nguyễn 🎉");

        displayName.Value.Should().Be("Nguyễn 🎉");
    }
}
