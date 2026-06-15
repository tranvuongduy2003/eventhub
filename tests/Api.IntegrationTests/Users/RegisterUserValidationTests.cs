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
public sealed class RegisterUserValidationTests(IntegrationTestFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient(
        new WebApplicationFactoryClientOptions { HandleCookies = true });

    [Fact]
    public async Task RegisterUserCommand_ValidationFailure_ReturnsResultWithoutThrowing()
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var act = async () => await sender.Send(
            new RegisterUserCommand("   ", "not-an-email", "short1"));

        var result = await act.Should().NotThrowAsync();
        result.Subject.IsFailure.Should().BeTrue();
        result.Subject.Error!.Code.Should().Be(Error.ValidationFailedCode);
    }

    [Fact]
    public async Task RegisterUser_DisplayNameEmpty_Returns422_VALIDATION_FAILED()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];

        using var response = await _client.PostAsJsonAsync(
            "/api/users",
            new RegisterUserRequest("   ", $"user_{suffix}@example.com", "SecurePass1!"));

        await RegisterUserTestHelpers.AssertValidationFailedAsync(
            response,
            assertErrors: errors =>
            {
                errors.Should().ContainKey("displayName");
                errors["displayName"].Should().NotBeEmpty();
            });
    }

    [Fact]
    public async Task RegisterUser_InvalidEmail_Returns422()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];

        using var response = await _client.PostAsJsonAsync(
            "/api/users",
            new RegisterUserRequest($"User {suffix}", "not-an-email", "SecurePass1!"));

        await RegisterUserTestHelpers.AssertValidationFailedAsync(
            response,
            assertErrors: errors =>
            {
                errors.Should().ContainKey("email");
                errors["email"].Should().NotBeEmpty();
            });
    }

    [Fact]
    public async Task RegisterUser_WeakPassword_Returns422_AllPasswordRulesListed()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];

        using var response = await _client.PostAsJsonAsync(
            "/api/users",
            new RegisterUserRequest($"User {suffix}", $"user_{suffix}@example.com", "short1"));

        await RegisterUserTestHelpers.AssertValidationFailedAsync(
            response,
            assertErrors: errors =>
            {
                errors.Should().ContainKey("password");
                errors["password"].Should().HaveCountGreaterThanOrEqualTo(2);
            });
    }

    [Fact]
    public async Task RegisterUser_DisplayNameWithSpaces_Returns201()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var displayName = $"Jane Organizer {suffix}";

        using var response = await _client.PostAsJsonAsync(
            "/api/users",
            new RegisterUserRequest(displayName, $"user_{suffix}@example.com", "SecurePass1!"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var registration = await response.Content.ReadFromJsonAsync<UserRegistrationResponse>();
        registration.Should().NotBeNull();
        registration!.DisplayName.Should().Be(displayName);
    }

    [Fact]
    public async Task RegisterUser_MalformedJson_Returns400_INVALID_REQUEST()
    {
        using var response = await _client.PostAsync(
            "/api/users",
            RegisterUserTestHelpers.JsonContent("{\"displayName\":"));

        await RegisterUserTestHelpers.AssertInvalidRequestAsync(response);
    }

    [Fact]
    public async Task RegisterUser_MissingRequiredFields_Returns400_INVALID_REQUEST()
    {
        using var response = await _client.PostAsync(
            "/api/users",
            RegisterUserTestHelpers.JsonContent("{}"));

        await RegisterUserTestHelpers.AssertInvalidRequestAsync(response);
    }

    [Fact]
    public async Task RegisterUser_ValidationFailure_DoesNotInsertRows()
    {
        var countsBefore = await CountPersistenceRowsAsync();

        using var response = await _client.PostAsJsonAsync(
            "/api/users",
            new RegisterUserRequest("   ", "not-an-email", "short1"));

        await RegisterUserTestHelpers.AssertValidationFailedAsync(response);

        var countsAfter = await CountPersistenceRowsAsync();
        countsAfter.Should().Be(countsBefore);
    }

    [Fact]
    public async Task RegisterUser_EmailTrimmed_Succeeds()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"  jane_{suffix}@example.com  ";

        using var response = await _client.PostAsJsonAsync(
            "/api/users",
            new RegisterUserRequest($"Jane {suffix}", email, "SecurePass1!"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var registration = await response.Content.ReadFromJsonAsync<UserRegistrationResponse>();
        registration.Should().NotBeNull();
        registration!.Email.Should().Be(email.Trim());
    }

    private async Task<int> CountPersistenceRowsAsync()
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        return await databaseContext.Users.CountAsync();
    }
}
