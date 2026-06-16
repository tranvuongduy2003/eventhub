using EventHub.Domain.Events;
using EventHub.Domain.Users;
using FluentAssertions;

namespace EventHub.Domain.UnitTests.Users;

public class UserUpdateProfileTests
{
    private static User CreateTestUser() =>
        User.Register(
            DisplayName.Create("Original Name"),
            EmailAddress.Create("original@example.com"),
            PasswordHash.Create("hashed-password"),
            DateTimeOffset.UtcNow);

    [Fact]
    public void UpdateProfile_WithNewDisplayName_ShouldUpdateDisplayName()
    {
        var user = CreateTestUser();

        user.UpdateProfile(DisplayName.Create("New Name"), null);

        user.DisplayName.Value.Should().Be("New Name");
    }

    [Fact]
    public void UpdateProfile_WithNewEmail_ShouldUpdateEmail()
    {
        var user = CreateTestUser();

        user.UpdateProfile(null, EmailAddress.Create("new@example.com"));

        user.Email.Value.Should().Be("new@example.com");
    }

    [Fact]
    public void UpdateProfile_WithSameEmail_ShouldBeNoOp()
    {
        var user = CreateTestUser();
        user.ClearDomainEvents();

        user.UpdateProfile(null, EmailAddress.Create("original@example.com"));

        user.Email.Value.Should().Be("original@example.com");
        user.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void UpdateProfile_WithBothFields_ShouldUpdateBoth()
    {
        var user = CreateTestUser();

        user.UpdateProfile(
            DisplayName.Create("Updated Name"),
            EmailAddress.Create("updated@example.com"));

        user.DisplayName.Value.Should().Be("Updated Name");
        user.Email.Value.Should().Be("updated@example.com");
    }

    [Fact]
    public void UpdateProfile_WithNullFields_ShouldNotChangeAndShouldNotRaiseEvent()
    {
        var user = CreateTestUser();
        user.ClearDomainEvents();

        user.UpdateProfile(null, null);

        user.DisplayName.Value.Should().Be("Original Name");
        user.Email.Value.Should().Be("original@example.com");
    }

    [Fact]
    public void UpdateProfile_ShouldRaiseUserProfileUpdatedEvent()
    {
        var user = CreateTestUser();
        user.ClearDomainEvents();

        user.UpdateProfile(DisplayName.Create("New Name"), null);

        user.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserProfileUpdatedEvent>()
            .Which.UserId.Should().Be(user.Id);
    }

    [Fact]
    public void SetAvatar_ShouldSetAvatarImageRef()
    {
        var user = CreateTestUser();
        var avatarRef = AvatarImageRef.Create("avatars/user-123/photo.jpg");

        user.SetAvatar(avatarRef);

        user.AvatarImageRef.Should().Be(avatarRef);
    }

    [Fact]
    public void SetAvatar_ShouldRaiseEvent()
    {
        var user = CreateTestUser();
        user.ClearDomainEvents();
        var avatarRef = AvatarImageRef.Create("avatars/user-123/photo.jpg");

        user.SetAvatar(avatarRef);

        user.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserProfileUpdatedEvent>();
    }

    [Fact]
    public void RemoveAvatar_WithExistingAvatar_ShouldClearAvatar()
    {
        var user = CreateTestUser();
        user.SetAvatar(AvatarImageRef.Create("avatars/user-123/photo.jpg"));
        user.ClearDomainEvents();

        user.RemoveAvatar();

        user.AvatarImageRef.Should().BeNull();
    }

    [Fact]
    public void RemoveAvatar_WithExistingAvatar_ShouldRaiseEvent()
    {
        var user = CreateTestUser();
        user.SetAvatar(AvatarImageRef.Create("avatars/user-123/photo.jpg"));
        user.ClearDomainEvents();

        user.RemoveAvatar();

        user.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserProfileUpdatedEvent>();
    }

    [Fact]
    public void RemoveAvatar_WithoutAvatar_ShouldBeNoOp()
    {
        var user = CreateTestUser();
        user.ClearDomainEvents();

        user.RemoveAvatar();

        user.AvatarImageRef.Should().BeNull();
        user.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void UpdateProfile_ShouldUpdateTimestamp()
    {
        var user = CreateTestUser();
        var originalTimestamp = user.UpdatedAt;

        user.UpdateProfile(DisplayName.Create("New Name"), null);

        user.UpdatedAt.Should().BeAfter(originalTimestamp);
    }
}
