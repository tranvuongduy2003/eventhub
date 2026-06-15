using System.Net;
using System.Net.Http.Json;
using EventHub.Api.IntegrationTests.Integration;
using EventHub.Api.IntegrationTests.Users.Fakes;
using EventHub.Application.Abstractions.Cache;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Options;
using EventHub.Contracts.Users;
using EventHub.Infrastructure.Persistence;
using EventHub.Testing.Common.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace EventHub.Api.IntegrationTests.Users;

[Collection(IntegrationTestCollection.Name)]
public sealed class SessionPersistenceTests(IntegrationTestFixture fixture)
{
    private static readonly DateTimeOffset SessionLifetimeTestStart =
        new(2026, 5, 25, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SessionPersistence_AfterLogin_SecondMeRequest_Returns200()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"persist_{suffix}@example.com";
        const string password = "SecurePass1!";

        var client = fixture.Factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = true });

        using var registerResponse = await client.PostAsJsonAsync(
            "/api/users",
            new RegisterUserRequest($"user_{suffix}", email, password));

        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var registration = await registerResponse.Content.ReadFromJsonAsync<UserRegistrationResponse>();
        registration.Should().NotBeNull();

        using var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginUserRequest(email, password));

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        loginResponse.Headers.Should().ContainKey("Set-Cookie");

        using var firstMeResponse = await client.GetAsync("/api/auth/me");
        firstMeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var firstMe = await firstMeResponse.Content.ReadFromJsonAsync<LoginUserResponse>();
        firstMe.Should().NotBeNull();
        firstMe!.UserId.Should().Be(registration!.UserId);

        using var secondMeResponse = await client.GetAsync("/api/auth/me");
        secondMeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondMe = await secondMeResponse.Content.ReadFromJsonAsync<LoginUserResponse>();
        secondMe.Should().NotBeNull();
        secondMe!.UserId.Should().Be(registration.UserId);
    }

    [Fact]
    public async Task SessionPersistence_WithinLifetime_Returns200()
    {
        var clock = new TestClock { UtcNow = SessionLifetimeTestStart };

        await using var factory = fixture.CreateFactory(services =>
        {
            services.RemoveAll<IClock>();
            services.AddSingleton<IClock>(clock);
            services.AddSingleton(clock);
        });

        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = true });

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"lifetime_{suffix}@example.com";
        const string password = "SecurePass1!";

        using var registerResponse = await client.PostAsJsonAsync(
            "/api/users",
            new RegisterUserRequest($"user_{suffix}", email, password));

        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var registration = await registerResponse.Content.ReadFromJsonAsync<UserRegistrationResponse>();
        registration.Should().NotBeNull();

        using var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginUserRequest(email, password));

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var meAtLoginResponse = await client.GetAsync("/api/auth/me");
        meAtLoginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        clock.UtcNow = SessionLifetimeTestStart.AddHours(1);

        using var meAfterOneHourResponse = await client.GetAsync("/api/auth/me");
        meAfterOneHourResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var me = await meAfterOneHourResponse.Content.ReadFromJsonAsync<LoginUserResponse>();
        me.Should().NotBeNull();
        me!.UserId.Should().Be(registration!.UserId);
    }

    [Fact]
    public async Task SessionPersistence_ExpiredSession_Returns401()
    {
        var clock = new TestClock { UtcNow = SessionLifetimeTestStart };

        await using var factory = fixture.CreateFactory(services =>
        {
            services.RemoveAll<IClock>();
            services.AddSingleton<IClock>(clock);
            services.AddSingleton(clock);
        });

        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = true });

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"expired_{suffix}@example.com";
        const string password = "SecurePass1!";

        using var registerResponse = await client.PostAsJsonAsync(
            "/api/users",
            new RegisterUserRequest($"user_{suffix}", email, password));

        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        using var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginUserRequest(email, password));

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var meBeforeExpiryResponse = await client.GetAsync("/api/auth/me");
        meBeforeExpiryResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var expirationHours = factory.Services
            .GetRequiredService<IOptions<AuthSessionOptions>>()
            .Value.ExpirationHours;

        clock.UtcNow = SessionLifetimeTestStart
            .AddHours(expirationHours)
            .AddMinutes(1);

        using var meAfterExpiryResponse = await client.GetAsync("/api/auth/me");
        meAfterExpiryResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SessionPersistence_RevokedSession_Returns401()
    {
        var client = fixture.Factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = true });

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"revoked_{suffix}@example.com";
        const string password = "SecurePass1!";

        using var registerResponse = await client.PostAsJsonAsync(
            "/api/users",
            new RegisterUserRequest($"user_{suffix}", email, password));

        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        using var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginUserRequest(email, password));

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var meBeforeRevokeResponse = await client.GetAsync("/api/auth/me");
        meBeforeRevokeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var sessionId = await GetLatestSessionIdForEmailAsync(fixture.Factory, email);
        sessionId.Should().NotBe(Guid.Empty);

        var revokedAt = DateTimeOffset.UtcNow;
        await RevokeSessionAsync(fixture.Factory, sessionId, revokedAt);

        using var meAfterRevokeResponse = await client.GetAsync("/api/auth/me");
        meAfterRevokeResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SessionPersistence_RedisCacheWriteFails_StillAuthenticatesViaPostgres()
    {
        await using var factory = fixture.CreateFactory(ConfigureThrowingCacheService);
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = true });

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"redis_{suffix}@example.com";
        const string password = "SecurePass1!";

        using var registerResponse = await client.PostAsJsonAsync(
            "/api/users",
            new RegisterUserRequest($"user_{suffix}", email, password));

        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var registration = await registerResponse.Content.ReadFromJsonAsync<UserRegistrationResponse>();
        registration.Should().NotBeNull();

        using var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginUserRequest(email, password));

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var meResponse = await client.GetAsync("/api/auth/me");
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var me = await meResponse.Content.ReadFromJsonAsync<LoginUserResponse>();
        me.Should().NotBeNull();
        me!.UserId.Should().Be(registration!.UserId);
    }

    private static void ConfigureThrowingCacheService(IServiceCollection services)
    {
        services.RemoveAll<ICacheService>();
        services.AddSingleton<ICacheService, ThrowingCacheService>();
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

    private static async Task RevokeSessionAsync(
        IntegrationTestWebApplicationFactory factory,
        Guid sessionId,
        DateTimeOffset revokedAt)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        await databaseContext.UserSessions
            .Where(session => session.Id == sessionId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(session => session.RevokedAt, revokedAt));
    }
}
