using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Solution.Api.Common;
using Solution.Api.IntegrationTests.Integration;
using Solution.Contracts.Users;
using Solution.Infrastructure.Persistence;
using Solution.Testing.Common.Fixtures;

namespace Solution.Api.IntegrationTests.Users;

[Collection(IntegrationTestCollection.Name)]
public sealed class RegisterUserDuplicateTests(IntegrationTestFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient(
        new WebApplicationFactoryClientOptions { HandleCookies = true });

    [Fact]
    public async Task RegisterUser_DuplicateUsername_Returns422_USERNAME_TAKEN()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var username = $"trader_{suffix}";
        var first = new RegisterUserRequest(username, $"first_{suffix}@example.com", "SecurePass1!");
        var second = new RegisterUserRequest(username, $"second_{suffix}@example.com", "SecurePass1!");

        using (var created = await _client.PostAsJsonAsync("/api/users", first))
        {
            created.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        using var duplicate = await _client.PostAsJsonAsync("/api/users", second);
        await AssertValidationProblemAsync(duplicate, "USERNAME_TAKEN");
    }

    [Fact]
    public async Task RegisterUser_DuplicateEmail_Returns422_EMAIL_TAKEN()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"trader_{suffix}@example.com";
        var first = new RegisterUserRequest($"user_a_{suffix}", email, "SecurePass1!");
        var second = new RegisterUserRequest($"user_b_{suffix}", email, "SecurePass1!");

        using (var created = await _client.PostAsJsonAsync("/api/users", first))
        {
            created.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        using var duplicate = await _client.PostAsJsonAsync("/api/users", second);
        await AssertValidationProblemAsync(duplicate, "EMAIL_TAKEN");
    }

    [Fact]
    public async Task RegisterUser_AfterDuplicateFix_Returns201()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var takenUsername = $"taken_{suffix}";
        var takenEmail = $"taken_{suffix}@example.com";
        var alternateEmail = $"alt_{suffix}@example.com";
        var alternateUsername = $"alt_{suffix}";

        using (var created = await _client.PostAsJsonAsync(
            "/api/users",
            new RegisterUserRequest(takenUsername, takenEmail, "SecurePass1!")))
        {
            created.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        using (var duplicateUsername = await _client.PostAsJsonAsync(
            "/api/users",
            new RegisterUserRequest(takenUsername, alternateEmail, "SecurePass1!")))
        {
            await AssertValidationProblemAsync(duplicateUsername, "USERNAME_TAKEN");
        }

        using (var duplicateEmail = await _client.PostAsJsonAsync(
            "/api/users",
            new RegisterUserRequest(alternateUsername, takenEmail, "SecurePass1!")))
        {
            await AssertValidationProblemAsync(duplicateEmail, "EMAIL_TAKEN");
        }

        using var success = await _client.PostAsJsonAsync(
            "/api/users",
            new RegisterUserRequest(alternateUsername, alternateEmail, "SecurePass1!"));

        success.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task RegisterUser_DuplicateFailure_DoesNotInsertOrphanRows()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var username = $"trader_{suffix}";
        var first = new RegisterUserRequest(username, $"first_{suffix}@example.com", "SecurePass1!");
        var second = new RegisterUserRequest(username, $"second_{suffix}@example.com", "SecurePass1!");

        using (var created = await _client.PostAsJsonAsync("/api/users", first))
        {
            created.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        var countsBefore = await CountPersistenceRowsAsync();

        using var duplicate = await _client.PostAsJsonAsync("/api/users", second);
        await AssertValidationProblemAsync(duplicate, "USERNAME_TAKEN");

        var countsAfter = await CountPersistenceRowsAsync();
        countsAfter.Should().Be(countsBefore);
    }

    private async Task<int> CountPersistenceRowsAsync()
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        return await databaseContext.Users.CountAsync();
    }

    private static async Task AssertValidationProblemAsync(HttpResponseMessage response, string expectedCode)
    {
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var problem = await response.Content.ReadFromJsonAsync<ApiProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(422);
        problem.Code.Should().Be(expectedCode);
    }
}
