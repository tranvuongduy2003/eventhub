using System.Net;
using System.Net.Http.Json;
using EventHub.Api.IntegrationTests.Integration;
using EventHub.Contracts.Users;
using EventHub.Infrastructure.Persistence;
using EventHub.Testing.Common.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.Api.IntegrationTests.Users;

[Collection(IntegrationTestCollection.Name)]
public sealed class RegisterUserSessionTests(IntegrationTestFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient(
        new WebApplicationFactoryClientOptions { HandleCookies = true });

    [Fact]
    public async Task GetCurrentUser_WithoutSession_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RegisterUser_ThenGetCurrentUser_ReturnsSameUser()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var displayName = $"Session User {suffix}";
        var email = $"session_{suffix}@example.com";

        using var registerResponse = await _client.PostAsJsonAsync(
            "/api/users",
            new RegisterUserRequest(displayName, email, "SecurePass1!"));

        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var registration = await registerResponse.Content.ReadFromJsonAsync<UserRegistrationResponse>();
        registration.Should().NotBeNull();

        using var meResponse = await _client.GetAsync("/api/auth/me");

        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var currentUser = await meResponse.Content.ReadFromJsonAsync<LoginUserResponse>();
        currentUser.Should().NotBeNull();
        currentUser!.UserId.Should().Be(registration!.UserId);
        currentUser.DisplayName.Should().Be(displayName);
        currentUser.Email.Should().Be(email);
    }

    [Fact]
    public async Task RegisterUser_PasswordStoredAsHash_NotPlaintext()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        const string password = "SecurePass1!";
        var email = $"hash_{suffix}@example.com";

        using var registerResponse = await _client.PostAsJsonAsync(
            "/api/users",
            new RegisterUserRequest($"Hash User {suffix}", email, password));

        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        var userRecord = await databaseContext.Users
            .AsNoTracking()
            .SingleAsync(user => user.Email == email.ToLowerInvariant());

        userRecord.PasswordHash.Should().NotBe(password);
        userRecord.PasswordHash.Should().NotBeNullOrWhiteSpace();
        userRecord.PasswordHash.Length.Should().BeGreaterThan(password.Length);
    }
}
