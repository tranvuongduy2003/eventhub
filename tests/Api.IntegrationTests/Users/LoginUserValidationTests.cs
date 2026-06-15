using System.Net;
using System.Net.Http.Json;
using EventHub.Api.IntegrationTests.Integration;
using EventHub.Application.Common;
using EventHub.Application.Users.Commands;
using EventHub.Contracts.Users;
using EventHub.Infrastructure.Persistence;
using EventHub.Testing.Common.Fixtures;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.Api.IntegrationTests.Users;

[Collection(IntegrationTestCollection.Name)]
public sealed class LoginUserValidationTests(IntegrationTestFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient(
        new WebApplicationFactoryClientOptions { HandleCookies = true });

    [Fact]
    public async Task LoginUserCommand_ValidationFailure_ReturnsResultWithoutThrowing()
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var act = async () => await sender.Send(new LoginUserCommand("not-an-email", "SecurePass1!"));

        var result = await act.Should().NotThrowAsync();
        result.Subject.IsFailure.Should().BeTrue();
        result.Subject.Error!.Code.Should().Be(Error.ValidationFailedCode);
    }

    [Fact]
    public async Task LoginUser_InvalidEmail_Returns422()
    {
        using var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginUserRequest("not-an-email", "SecurePass1!"));

        await LoginUserTestHelpers.AssertValidationFailedAsync(
            response,
            assertErrors: errors =>
            {
                errors.Should().ContainKey("email");
                errors["email"].Should().NotBeEmpty();
            });
    }

    [Fact]
    public async Task LoginUser_EmptyPassword_Returns422()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"login_{suffix}@example.com";

        using var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginUserRequest(email, string.Empty));

        await LoginUserTestHelpers.AssertValidationFailedAsync(
            response,
            assertErrors: errors =>
            {
                errors.Should().ContainKey("password");
                errors["password"].Should().NotBeEmpty();
            });
    }

    [Fact]
    public async Task LoginUser_ValidationFailure_DoesNotInsert_UserSession()
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        var sessionsBefore = await databaseContext.UserSessions.CountAsync();

        using var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginUserRequest("not-an-email", "SecurePass1!"));

        await LoginUserTestHelpers.AssertValidationFailedAsync(response);

        (await databaseContext.UserSessions.CountAsync()).Should().Be(sessionsBefore);
    }

    [Fact]
    public async Task LoginUser_SingleSubmit_InsertsOneSession()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"login_{suffix}@example.com";
        const string password = "SecurePass1!";

        using var registerClient = fixture.Factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = true });

        using var registerResponse = await registerClient.PostAsJsonAsync(
            "/api/users",
            new RegisterUserRequest($"trader_{suffix}", email, password));

        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var registration = await registerResponse.Content.ReadFromJsonAsync<UserRegistrationResponse>();
        registration.Should().NotBeNull();

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        var sessionsForUserBefore = await databaseContext.UserSessions.CountAsync(
            session => session.UserId == registration!.UserId);

        using var loginClient = fixture.Factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = true });

        using var loginResponse = await loginClient.PostAsJsonAsync(
            "/api/auth/login",
            new LoginUserRequest(email, password));

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        loginResponse.Headers.Should().ContainKey("Set-Cookie");

        var sessionsForUserAfter = await databaseContext.UserSessions.CountAsync(
            session => session.UserId == registration.UserId);

        sessionsForUserAfter.Should().Be(sessionsForUserBefore + 1);
    }
}
