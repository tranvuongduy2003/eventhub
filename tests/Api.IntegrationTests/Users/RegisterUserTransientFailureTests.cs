using System.Net;
using System.Net.Http.Json;
using EventHub.Api.Common;
using EventHub.Api.IntegrationTests.Integration;
using EventHub.Api.IntegrationTests.Users.Fakes;
using EventHub.Application.Abstractions.Cache;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Users.Commands;
using EventHub.Contracts.Users;
using EventHub.Infrastructure.Persistence;
using EventHub.Testing.Common.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EventHub.Api.IntegrationTests.Users;

[Collection(IntegrationTestCollection.Name)]
public sealed class RegisterUserTransientFailureTests(IntegrationTestFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient(
        new WebApplicationFactoryClientOptions { HandleCookies = true });

    [Fact]
    public async Task RegisterUser_RetrySameCredentials_Returns422_NotSecondUser()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var displayName = $"Retry User {suffix}";
        var email = $"retry_{suffix}@example.com";
        var request = new RegisterUserRequest(displayName, email, "SecurePass1!");

        var userCountBefore = await CountUsersAsync();

        using (var first = await _client.PostAsJsonAsync("/api/users", request))
        {
            first.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        var userCountAfterFirst = await CountUsersAsync();
        userCountAfterFirst.Should().Be(userCountBefore + 1);

        using var retry = await _client.PostAsJsonAsync("/api/users", request);

        retry.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var problem = await retry.Content.ReadFromJsonAsync<ApiProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Code.Should().Be("EMAIL_TAKEN");

        var userCountAfterRetry = await CountUsersAsync();
        userCountAfterRetry.Should().Be(userCountAfterFirst);
    }

    [Fact]
    public async Task RegisterUser_WhenPersistenceFails_Returns500_INTERNAL_ERROR()
    {
        await using var factory = fixture.CreateFactory(ConfigureThrowOnAddUserRepository);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var request = new RegisterUserRequest(
            $"Fail User {suffix}",
            $"fail_{suffix}@example.com",
            "SecurePass1!");

        var usersBefore = await CountUsersAsync(factory);

        using var response = await client.PostAsJsonAsync("/api/users", request);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        var problem = await response.Content.ReadFromJsonAsync<ApiProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(500);
        problem.Code.Should().Be("INTERNAL_ERROR");

        var usersAfter = await CountUsersAsync(factory);
        usersAfter.Should().Be(usersBefore);
    }

    [Fact]
    public async Task RegisterUser_WhenRedisCacheWriteFails_StillReturns201()
    {
        await using var factory = fixture.CreateFactory(ConfigureThrowingCacheService);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var request = new RegisterUserRequest(
            $"Redis User {suffix}",
            $"redis_{suffix}@example.com",
            "SecurePass1!");

        using var registerResponse = await client.PostAsJsonAsync("/api/users", request);

        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var registration = await registerResponse.Content.ReadFromJsonAsync<UserRegistrationResponse>();
        registration.Should().NotBeNull();
        registration!.UserId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RegisterUser_ParallelSameEmail_AtMostOneUser()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"parallel_{suffix}@example.com";
        var request = new RegisterUserRequest(
            $"Parallel User {suffix}",
            email,
            "SecurePass1!");

        var userCountBefore = await CountUsersAsync();

        using var clientOne = fixture.Factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = true });
        using var clientTwo = fixture.Factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = true });

        var responses = await Task.WhenAll(
            clientOne.PostAsJsonAsync("/api/users", request),
            clientTwo.PostAsJsonAsync("/api/users", request));

        try
        {
            var createdCount = responses.Count(response => response.StatusCode == HttpStatusCode.Created);
            createdCount.Should().Be(1);

            var userCountAfter = await CountUsersAsync();
            userCountAfter.Should().Be(userCountBefore + 1);

            var nonCreated = responses.Where(response => response.StatusCode != HttpStatusCode.Created).ToArray();
            nonCreated.Should().ContainSingle();

            nonCreated[0].StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

            var problem = await nonCreated[0].Content.ReadFromJsonAsync<ApiProblemDetails>();
            problem.Should().NotBeNull();
            problem!.Code.Should().Be(RegistrationErrors.EmailTakenCode);
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }
    }

    private static void ConfigureThrowOnAddUserRepository(IServiceCollection services)
    {
        services.RemoveAll<IUserRepository>();
        services.AddScoped<IUserRepository, ThrowOnAddUserRepository>();
    }

    private static void ConfigureThrowingCacheService(IServiceCollection services)
    {
        services.RemoveAll<ICacheService>();
        services.AddSingleton<ICacheService, ThrowingCacheService>();
    }

    private async Task<int> CountUsersAsync()
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        return await databaseContext.Users.CountAsync();
    }

    private static async Task<int> CountUsersAsync(IntegrationTestWebApplicationFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        return await databaseContext.Users.CountAsync();
    }
}
