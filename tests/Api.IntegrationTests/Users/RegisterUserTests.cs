using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Solution.Api.IntegrationTests.Integration;
using Solution.Contracts.Users;
using Solution.Testing.Common.Fixtures;

namespace Solution.Api.IntegrationTests.Users;

[Collection(IntegrationTestCollection.Name)]
public sealed class RegisterUserTests(IntegrationTestFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient(
        new WebApplicationFactoryClientOptions { HandleCookies = true });

    [Fact]
    public async Task RegisterUser_Returns201_AndSetsSessionCookie()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var request = new RegisterUserRequest(
            $"user_{suffix}",
            $"user_{suffix}@example.com",
            "SecurePass1!");

        using var registerResponse = await _client.PostAsJsonAsync("/api/users", request);

        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        registerResponse.Headers.Should().ContainKey("Set-Cookie");

        var registration = await registerResponse.Content.ReadFromJsonAsync<UserRegistrationResponse>();
        registration.Should().NotBeNull();
        registration!.Username.Should().Be($"user_{suffix}");
        registration.Email.Should().Be($"user_{suffix}@example.com");
        registration.UserId.Should().NotBeEmpty();
    }
}
