using System.Net;
using System.Net.Http.Json;
using EventHub.Api.IntegrationTests.Integration;
using EventHub.Contracts.Users;
using EventHub.Testing.Common.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EventHub.Api.IntegrationTests.Users;

[Collection(IntegrationTestCollection.Name)]
public sealed class RemoveAvatarTests(IntegrationTestFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient(
        new WebApplicationFactoryClientOptions { HandleCookies = true });

    [Fact]
    public async Task RemoveAvatar_WhenNoAvatar_Returns204()
    {
        await RegisterUserAsync();

        using var response = await _client.DeleteAsync("/api/users/me/avatar");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RemoveAvatar_WhenNotAuthenticated_Returns401()
    {
        using var client = fixture.Factory.CreateClient();
        using var response = await client.DeleteAsync("/api/users/me/avatar");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RemoveAvatar_AfterUpload_Returns204_AndAvatarUrlIsNullOnNextMe()
    {
        await RegisterUserAsync();

        using var uploadContent = CreateMultipartContent("avatar.jpg", "image/jpeg");
        using var uploadResponse = await _client.PostAsync("/api/users/me/avatar", uploadContent);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var removeResponse = await _client.DeleteAsync("/api/users/me/avatar");
        removeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var meResponse = await _client.GetAsync("/api/auth/me");
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var currentUser = await meResponse.Content.ReadFromJsonAsync<LoginUserResponse>();
        currentUser.Should().NotBeNull();
        currentUser!.AvatarUrl.Should().BeNull();
    }

    private static MultipartFormDataContent CreateMultipartContent(string fileName, string contentType)
    {
        var fileContent = new byte[1024];
        Random.Shared.NextBytes(fileContent);

        var fileStream = new MemoryStream(fileContent);
        var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

        var multipartContent = new MultipartFormDataContent();
        multipartContent.Add(streamContent, "file", fileName);

        return multipartContent;
    }

    private async Task<Guid> RegisterUserAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var request = new RegisterUserRequest(
            $"Test User {suffix}",
            $"test_{suffix}@example.com",
            "SecurePass1!");

        using var response = await _client.PostAsJsonAsync("/api/users", request);
        response.EnsureSuccessStatusCode();
        var registration = await response.Content.ReadFromJsonAsync<UserRegistrationResponse>();
        return registration!.UserId;
    }
}
