using System.Net;
using System.Net.Http.Json;
using EventHub.Api.IntegrationTests.Integration;
using EventHub.Contracts.Users;
using EventHub.Testing.Common.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EventHub.Api.IntegrationTests.Users;

[Collection(IntegrationTestCollection.Name)]
public sealed class RegisterUserTests(IntegrationTestFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient(
        new WebApplicationFactoryClientOptions { HandleCookies = true });

    [Fact]
    public async Task RegisterUser_Returns201_AndSetsSessionCookie()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var displayName = $"Organizer {suffix}";
        var request = new RegisterUserRequest(
            displayName,
            $"user_{suffix}@example.com",
            "SecurePass1!");

        using var registerResponse = await _client.PostAsJsonAsync("/api/users", request);

        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        registerResponse.Headers.Should().ContainKey("Set-Cookie");

        var registration = await registerResponse.Content.ReadFromJsonAsync<UserRegistrationResponse>();
        registration.Should().NotBeNull();
        registration!.DisplayName.Should().Be(displayName);
        registration.Email.Should().Be($"user_{suffix}@example.com");
        registration.UserId.Should().NotBeEmpty();
    }
}
