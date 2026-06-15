using System.Net;
using System.Net.Http.Json;
using EventHub.Api.IntegrationTests.Integration;
using EventHub.Application.Abstractions.Cache;
using EventHub.Contracts.Users;
using EventHub.Infrastructure.Persistence;
using EventHub.Testing.Common.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.Api.IntegrationTests.Users;

[Collection(IntegrationTestCollection.Name)]
public sealed class LogoutUserTests(IntegrationTestFixture fixture)
{
    private HttpClient CreateClient() =>
        fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

    [Fact]
    public async Task LogoutUser_Returns204_AndClearsCookie()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"logout_{suffix}@example.com";
        const string password = "SecurePass1!";

        var client = CreateClient();

        using var registerResponse = await client.PostAsJsonAsync(
            "/api/users",
            new RegisterUserRequest($"trader_{suffix}", email, password));

        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        using var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginUserRequest(email, password));

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        loginResponse.Headers.Should().ContainKey("Set-Cookie");

        using var meBeforeLogoutResponse = await client.GetAsync("/api/auth/me");
        meBeforeLogoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var sessionId = await GetLatestSessionIdForEmailAsync(fixture.Factory, email);
        sessionId.Should().NotBe(Guid.Empty);

        using var logoutResponse = await client.PostAsync("/api/auth/logout", null);

        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        logoutResponse.Headers.Should().ContainKey("Set-Cookie");

        var revokedAt = await GetRevokedAtAsync(fixture.Factory, sessionId);
        revokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task LogoutUser_AfterLogout_MeReturns401()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"logout_wallet_{suffix}@example.com";
        const string password = "SecurePass1!";

        var client = CreateClient();

        using var registerResponse = await client.PostAsJsonAsync(
            "/api/users",
            new RegisterUserRequest($"trader_{suffix}", email, password));

        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        using var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginUserRequest(email, password));

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var logoutResponse = await client.PostAsync("/api/auth/logout", null);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var meAfterLogoutResponse = await client.GetAsync("/api/auth/me");
        meAfterLogoutResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogoutUser_WithoutSession_Returns401()
    {
        using var client = fixture.Factory.CreateClient();

        using var logoutResponse = await client.PostAsync("/api/auth/logout", null);

        logoutResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var responseBody = await logoutResponse.Content.ReadAsStringAsync();
        responseBody.Should().NotContain("password", because: "must not leak sensitive fields");
    }

    [Fact]
    public async Task LogoutUser_SecondCallAfterLogout_Returns401()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"logout_twice_{suffix}@example.com";
        const string password = "SecurePass1!";

        var client = CreateClient();

        using var registerResponse = await client.PostAsJsonAsync(
            "/api/users",
            new RegisterUserRequest($"trader_{suffix}", email, password));

        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        using var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginUserRequest(email, password));

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var firstLogoutResponse = await client.PostAsync("/api/auth/logout", null);
        firstLogoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var secondLogoutResponse = await client.PostAsync("/api/auth/logout", null);
        secondLogoutResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogoutUser_RemovesRedisSessionKey()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"logout_redis_{suffix}@example.com";
        const string password = "SecurePass1!";

        var client = CreateClient();

        using var registerResponse = await client.PostAsJsonAsync(
            "/api/users",
            new RegisterUserRequest($"trader_{suffix}", email, password));

        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        using var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginUserRequest(email, password));

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var sessionId = await GetLatestSessionIdForEmailAsync(fixture.Factory, email);
        sessionId.Should().NotBe(Guid.Empty);

        var sessionKey = $"session:{sessionId:D}";

        await using (var scope = fixture.Factory.Services.CreateAsyncScope())
        {
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
            (await cacheService.KeyExistsAsync(sessionKey)).Should().BeTrue();
        }

        using var logoutResponse = await client.PostAsync("/api/auth/logout", null);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await using (var scope = fixture.Factory.Services.CreateAsyncScope())
        {
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
            (await cacheService.KeyExistsAsync(sessionKey)).Should().BeFalse();
        }
    }

    private static async Task<Guid> GetLatestSessionIdForEmailAsync(
        IntegrationTestWebApplicationFactory factory,
        string email)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        var sessionId = await (
            from session in databaseContext.UserSessions.AsNoTracking()
            join user in databaseContext.Users.AsNoTracking() on session.UserId equals user.Id
            where user.Email == email
            orderby session.CreatedAt descending
            select session.Id).FirstOrDefaultAsync();

        return sessionId;
    }

    private static async Task<DateTimeOffset?> GetRevokedAtAsync(
        IntegrationTestWebApplicationFactory factory,
        Guid sessionId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        return await databaseContext.UserSessions
            .AsNoTracking()
            .Where(session => session.Id == sessionId)
            .Select(session => session.RevokedAt)
            .FirstOrDefaultAsync();
    }
}
