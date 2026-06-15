using System.Net;
using System.Net.Http.Json;
using EventHub.Api.IntegrationTests.Integration;
using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Users;
using EventHub.Application.Users.Commands;
using EventHub.Contracts.Users;
using EventHub.Domain.Users;
using EventHub.Infrastructure.Persistence;
using EventHub.Testing.Common.Fixtures;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.Api.IntegrationTests.Users;

[Collection(IntegrationTestCollection.Name)]
public sealed class LoginUserTests(IntegrationTestFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient(
        new WebApplicationFactoryClientOptions { HandleCookies = true });

    private HttpClient CreateClient() =>
        fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

    [Fact]
    public async Task LoginUser_Returns200_AndSetsSessionCookie()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"login_{suffix}@example.com";
        const string password = "SecurePass1!";

        using var registerClient = CreateClient();
        using var loginClient = CreateClient();

        using var registerResponse = await registerClient.PostAsJsonAsync(
            "/api/users",
            new RegisterUserRequest($"user_{suffix}", email, password));

        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var registration = await registerResponse.Content.ReadFromJsonAsync<UserRegistrationResponse>();
        registration.Should().NotBeNull();

        using var loginResponse = await loginClient.PostAsJsonAsync(
            "/api/auth/login",
            new LoginUserRequest(email, password));

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        loginResponse.Headers.Should().ContainKey("Set-Cookie");

        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginUserResponse>();
        loginBody.Should().NotBeNull();
        loginBody!.UserId.Should().Be(registration!.UserId);
        loginBody.DisplayName.Should().Be(registration.DisplayName);
    }

    [Fact]
    public async Task LoginUser_EndpointExists_Returns401_InvalidCredentials()
    {
        var request = new LoginUserRequest("user@example.com", "SecurePass1!");

        using var client = CreateClient();
        using var response = await client.PostAsJsonAsync("/api/auth/login", request);

        await LoginUserTestHelpers.AssertInvalidCredentialsAsync(response);
    }

    [Fact]
    public async Task LoginUser_MalformedJson_Returns400_INVALID_REQUEST()
    {
        using var response = await _client.PostAsync(
            "/api/auth/login",
            RegisterUserTestHelpers.JsonContent("{"));

        await RegisterUserTestHelpers.AssertInvalidRequestAsync(response);
    }

    [Fact]
    public async Task PasswordHasher_Verify_ReturnsTrue_ForRegisteredPassword()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"login_{suffix}@example.com";
        const string password = "SecurePass1!";

        using var registerResponse = await _client.PostAsJsonAsync(
            "/api/users",
            new RegisterUserRequest($"user_{suffix}", email, password));

        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var normalizedEmail = EmailAddress.Create(email).Value;
        var user = await userRepository.GetByEmailAsync(normalizedEmail);

        user.Should().NotBeNull();
        passwordHasher.Verify(Password.Create(password), user!.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task UserRepository_GetByEmailAsync_ReturnsNull_WhenUnknown()
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var user = await userRepository.GetByEmailAsync($"missing_{Guid.NewGuid():N}@example.com");

        user.Should().BeNull();
    }

    [Fact]
    public async Task LoginUserCommand_Succeeds_ForRegisteredUser()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"login_{suffix}@example.com";
        const string password = "SecurePass1!";

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var registerResult = await sender.Send(
            new RegisterUserCommand($"user_{suffix}", email, password));

        registerResult.IsSuccess.Should().BeTrue();

        var loginResult = await sender.Send(new LoginUserCommand(email, password));

        loginResult.IsSuccess.Should().BeTrue();
        loginResult.Value!.UserId.Should().Be(registerResult.Value!.UserId);
        loginResult.Value.DisplayName.Should().Be($"user_{suffix}");
        loginResult.Value.SessionId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task LoginUserCommand_WrongPassword_ReturnsInvalidCredentials()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"login_{suffix}@example.com";

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var registerResult = await sender.Send(
            new RegisterUserCommand($"user_{suffix}", email, "SecurePass1!"));

        registerResult.IsSuccess.Should().BeTrue();

        var loginResult = await sender.Send(new LoginUserCommand(email, "WrongPass1!"));

        loginResult.IsFailure.Should().BeTrue();
        loginResult.Error!.Code.Should().Be(LoginErrors.InvalidCredentialsCode);
    }

    [Fact]
    public async Task LoginUser_UnknownEmail_Returns401_InvalidCredentials_NoSession()
    {
        var unknownEmail = $"unknown_{Guid.NewGuid():N}@example.com";

        using var client = CreateClient();
        using var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginUserRequest(unknownEmail, "SecurePass1!"));

        await LoginUserTestHelpers.AssertInvalidCredentialsAsync(loginResponse);
    }

    [Fact]
    public async Task LoginUser_MixedCaseEmail_Returns200_WhenNormalizedMatches()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"jane_{suffix}@example.com";
        var mixedCaseEmail = $"Jane_{suffix}@Example.COM";
        const string password = "SecurePass1!";

        using var registerClient = CreateClient();
        using var loginClient = CreateClient();

        using var registerResponse = await registerClient.PostAsJsonAsync(
            "/api/users",
            new RegisterUserRequest($"jane_{suffix}", email, password));

        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        using var loginResponse = await loginClient.PostAsJsonAsync(
            "/api/auth/login",
            new LoginUserRequest(mixedCaseEmail, password));

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        loginResponse.Headers.Should().ContainKey("Set-Cookie");
    }

    [Fact]
    public async Task LoginUser_FailedLogin_DoesNotInsert_UserSession()
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        var totalSessionsBefore = await databaseContext.UserSessions.CountAsync();

        using var unknownEmailClient = CreateClient();
        using var unknownEmailResponse = await unknownEmailClient.PostAsJsonAsync(
            "/api/auth/login",
            new LoginUserRequest($"unknown_{Guid.NewGuid():N}@example.com", "SecurePass1!"));

        await LoginUserTestHelpers.AssertInvalidCredentialsAsync(unknownEmailResponse);

        (await databaseContext.UserSessions.CountAsync()).Should().Be(totalSessionsBefore);
    }
}
