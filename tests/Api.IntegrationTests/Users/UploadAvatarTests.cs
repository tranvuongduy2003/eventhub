using System.Net;
using System.Net.Http.Json;
using EventHub.Api.IntegrationTests.Integration;
using EventHub.Contracts.Users;
using EventHub.Testing.Common.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EventHub.Api.IntegrationTests.Users;

[Collection(IntegrationTestCollection.Name)]
public sealed class UploadAvatarTests(IntegrationTestFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient(
        new WebApplicationFactoryClientOptions { HandleCookies = true });

    [Fact]
    public async Task UploadAvatar_WithValidJpeg_Returns200()
    {
        await RegisterUserAsync();

        using var content = CreateMultipartContent("avatar.jpg", "image/jpeg");
        using var response = await _client.PostAsync("/api/users/me/avatar", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UploadAvatarResponse>();
        result.Should().NotBeNull();
        result!.AvatarUrl.Should().Contain("avatars/");
    }

    [Fact]
    public async Task UploadAvatar_WithInvalidContentType_Returns422()
    {
        await RegisterUserAsync();

        using var content = CreateMultipartContent("document.pdf", "application/pdf");
        using var response = await _client.PostAsync("/api/users/me/avatar", content);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UploadAvatar_WhenNotAuthenticated_Returns401()
    {
        using var client = fixture.Factory.CreateClient();
        using var content = CreateMultipartContent("avatar.jpg", "image/jpeg");
        using var response = await client.PostAsync("/api/users/me/avatar", content);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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
