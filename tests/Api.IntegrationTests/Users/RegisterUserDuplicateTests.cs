using System.Net;
using System.Net.Http.Json;
using EventHub.Api.Common;
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
public sealed class RegisterUserDuplicateTests(IntegrationTestFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient(
        new WebApplicationFactoryClientOptions { HandleCookies = true });

    [Fact]
    public async Task RegisterUser_DuplicateDisplayName_AllowsBothAccounts()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var displayName = $"Organizer {suffix}";
        var first = new RegisterUserRequest(displayName, $"first_{suffix}@example.com", "SecurePass1!");
        var second = new RegisterUserRequest(displayName, $"second_{suffix}@example.com", "SecurePass1!");

        using (var created = await _client.PostAsJsonAsync("/api/users", first))
        {
            created.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        using var duplicateDisplayName = await _client.PostAsJsonAsync("/api/users", second);
        duplicateDisplayName.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task RegisterUser_DuplicateEmail_Returns422_EMAIL_TAKEN()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"trader_{suffix}@example.com";
        var first = new RegisterUserRequest($"User A {suffix}", email, "SecurePass1!");
        var second = new RegisterUserRequest($"User B {suffix}", email, "SecurePass1!");

        using (var created = await _client.PostAsJsonAsync("/api/users", first))
        {
            created.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        using var duplicate = await _client.PostAsJsonAsync("/api/users", second);
        await AssertValidationProblemAsync(
            duplicate,
            "EMAIL_TAKEN",
            errors => errors.Should().ContainKey("email"));
    }

    [Fact]
    public async Task RegisterUser_DuplicateEmailCaseInsensitive_Returns422_EMAIL_TAKEN()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"trader_{suffix}@example.com";
        var first = new RegisterUserRequest($"User A {suffix}", email, "SecurePass1!");
        var second = new RegisterUserRequest($"User B {suffix}", $"TRADER_{suffix}@EXAMPLE.COM", "SecurePass1!");

        using (var created = await _client.PostAsJsonAsync("/api/users", first))
        {
            created.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        using var duplicate = await _client.PostAsJsonAsync("/api/users", second);
        await AssertValidationProblemAsync(
            duplicate,
            "EMAIL_TAKEN",
            errors => errors.Should().ContainKey("email"));
    }

    [Fact]
    public async Task RegisterUser_AfterDuplicateFix_Returns201()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var takenEmail = $"taken_{suffix}@example.com";
        var alternateEmail = $"alt_{suffix}@example.com";

        using (var created = await _client.PostAsJsonAsync(
            "/api/users",
            new RegisterUserRequest($"First {suffix}", takenEmail, "SecurePass1!")))
        {
            created.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        using (var duplicateEmail = await _client.PostAsJsonAsync(
            "/api/users",
            new RegisterUserRequest($"Second {suffix}", takenEmail, "SecurePass1!")))
        {
            await AssertValidationProblemAsync(duplicateEmail, "EMAIL_TAKEN");
        }

        using var success = await _client.PostAsJsonAsync(
            "/api/users",
            new RegisterUserRequest($"Alternate {suffix}", alternateEmail, "SecurePass1!"));

        success.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task RegisterUser_DuplicateFailure_DoesNotInsertOrphanRows()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"trader_{suffix}@example.com";
        var first = new RegisterUserRequest($"User A {suffix}", email, "SecurePass1!");
        var second = new RegisterUserRequest($"User B {suffix}", email, "SecurePass1!");

        using (var created = await _client.PostAsJsonAsync("/api/users", first))
        {
            created.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        var countsBefore = await CountPersistenceRowsAsync();

        using var duplicate = await _client.PostAsJsonAsync("/api/users", second);
        await AssertValidationProblemAsync(duplicate, "EMAIL_TAKEN");

        var countsAfter = await CountPersistenceRowsAsync();
        countsAfter.Should().Be(countsBefore);
    }

    private async Task<int> CountPersistenceRowsAsync()
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        return await databaseContext.Users.CountAsync();
    }

    private static async Task AssertValidationProblemAsync(
        HttpResponseMessage response,
        string expectedCode,
        Action<IReadOnlyDictionary<string, string[]>>? assertErrors = null)
    {
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var problem = await response.Content.ReadFromJsonAsync<ApiProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(422);
        problem.Code.Should().Be(expectedCode);

        if (assertErrors is not null)
        {
            var errors = RegisterUserTestHelpers.ReadFieldErrors(problem);
            assertErrors(errors);
        }
    }
}
