using System.Net;
using System.Net.Http.Json;
using EventHub.Api.IntegrationTests.Integration;
using EventHub.Contracts.Users;
using EventHub.Testing.Common.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EventHub.Api.IntegrationTests.Users;

[Collection(IntegrationTestCollection.Name)]
public sealed class UpdateProfileTests(IntegrationTestFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient(
        new WebApplicationFactoryClientOptions { HandleCookies = true });

    [Fact]
    public async Task UpdateDisplayName_Returns200_WithUpdatedValues()
    {
        var userId = await RegisterUserAsync();

        var request = new UpdateProfileRequest("Updated Name", null);
        using var response = await _client.PatchAsJsonAsync("/api/users/me", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UpdateProfileResponse>();
        result.Should().NotBeNull();
        result!.DisplayName.Should().Be("Updated Name");
        result.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task UpdateEmail_Returns200_WithUpdatedEmail()
    {
        var userId = await RegisterUserAsync();

        var request = new UpdateProfileRequest(null, "newemail@example.com");
        using var response = await _client.PatchAsJsonAsync("/api/users/me", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UpdateProfileResponse>();
        result.Should().NotBeNull();
        result!.Email.Should().Be("newemail@example.com");
    }

    [Fact]
    public async Task UpdateEmail_WhenTakenByAnotherUser_Returns422()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        await RegisterUserAsync($"first_{suffix}@example.com");
        await RegisterUserAsync($"second_{suffix}@example.com");

        var request = new UpdateProfileRequest(null, $"first_{suffix}@example.com");
        using var response = await _client.PatchAsJsonAsync("/api/users/me", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UpdateDisplayName_WhenEmpty_Returns422()
    {
        await RegisterUserAsync();

        var request = new UpdateProfileRequest("", null);
        using var response = await _client.PatchAsJsonAsync("/api/users/me", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UpdateProfile_WhenNotAuthenticated_Returns401()
    {
        using var client = fixture.Factory.CreateClient();
        var request = new UpdateProfileRequest("New Name", null);
        using var response = await client.PatchAsJsonAsync("/api/users/me", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<Guid> RegisterUserAsync(string? email = null)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var request = new RegisterUserRequest(
            $"Test User {suffix}",
            email ?? $"test_{suffix}@example.com",
            "SecurePass1!");

        using var response = await _client.PostAsJsonAsync("/api/users", request);
        response.EnsureSuccessStatusCode();
        var registration = await response.Content.ReadFromJsonAsync<UserRegistrationResponse>();
        return registration!.UserId;
    }
}
