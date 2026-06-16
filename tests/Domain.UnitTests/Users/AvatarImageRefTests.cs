using EventHub.Domain.Exceptions;
using EventHub.Domain.Users;
using FluentAssertions;

namespace EventHub.Domain.UnitTests.Users;

public class AvatarImageRefTests
{
    [Fact]
    public void Create_WithValidValue_ShouldSucceed()
    {
        var avatarRef = AvatarImageRef.Create("avatars/user-123/photo.jpg");

        avatarRef.Value.Should().Be("avatars/user-123/photo.jpg");
    }

    [Fact]
    public void Create_WithWhitespaceValue_ShouldThrow()
    {
        var act = () => AvatarImageRef.Create("   ");

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("AVATAR_IMAGE_REF_EMPTY");
    }

    [Fact]
    public void Create_WithEmptyValue_ShouldThrow()
    {
        var act = () => AvatarImageRef.Create(string.Empty);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("AVATAR_IMAGE_REF_EMPTY");
    }

    [Fact]
    public void Create_WithValueExceedingMaxLength_ShouldThrow()
    {
        var longValue = new string('a', 513);

        var act = () => AvatarImageRef.Create(longValue);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("AVATAR_IMAGE_REF_TOO_LONG");
    }

    [Fact]
    public void Create_WithMaxLengthValue_ShouldSucceed()
    {
        var maxValue = new string('a', 512);

        var avatarRef = AvatarImageRef.Create(maxValue);

        avatarRef.Value.Should().Be(maxValue);
    }

    [Fact]
    public void Create_ShouldTrimValue()
    {
        var avatarRef = AvatarImageRef.Create("  avatars/user-123/photo.jpg  ");

        avatarRef.Value.Should().Be("avatars/user-123/photo.jpg");
    }

    [Fact]
    public void Equality_SameValue_ShouldBeEqual()
    {
        var ref1 = AvatarImageRef.Create("avatars/user-123/photo.jpg");
        var ref2 = AvatarImageRef.Create("avatars/user-123/photo.jpg");

        ref1.Should().Be(ref2);
    }

    [Fact]
    public void Equality_DifferentValue_ShouldNotBeEqual()
    {
        var ref1 = AvatarImageRef.Create("avatars/user-123/photo1.jpg");
        var ref2 = AvatarImageRef.Create("avatars/user-123/photo2.jpg");

        ref1.Should().NotBe(ref2);
    }
}
