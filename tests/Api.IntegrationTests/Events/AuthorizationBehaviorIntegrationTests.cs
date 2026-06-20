using System.Net;
using System.Net.Http.Json;
using EventHub.Api.Common;
using EventHub.Api.IntegrationTests.Integration;
using EventHub.Contracts.Users;
using EventHub.Domain.Events;
using EventHub.Domain.Users;
using EventHub.Infrastructure.Persistence;
using EventHub.Infrastructure.Persistence.Entities;
using EventHub.Testing.Common.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.Api.IntegrationTests.Events;

[Collection(IntegrationTestCollection.Name)]
public sealed class AuthorizationBehaviorIntegrationTests(IntegrationTestFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient(
        new WebApplicationFactoryClientOptions { HandleCookies = true });

    [Fact]
    public async Task Owner_CanAssignRoles()
    {
        var callerId = await RegisterUserAsync("rbac-owner");
        var targetId = await CreateUserInDatabaseAsync("rbac-target@example.com");
        var eventId = 400;
        await SeedOwnerRoleAsync(eventId, callerId);

        var request = new Contracts.Events.AssignRoleRequest(targetId, "Staff");
        using var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/roles", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Staff_CannotAssignRoles_Returns403WithInsufficientPermissions()
    {
        var callerId = await RegisterUserAsync("rbac-staff");
        var targetId = await CreateUserInDatabaseAsync("rbac-target2@example.com");
        var eventId = 401;
        await SeedRoleAsync(eventId, callerId, EventRole.Staff);

        var request = new Contracts.Events.AssignRoleRequest(targetId, "Staff");
        using var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/roles", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var problem = await response.Content.ReadFromJsonAsync<ApiProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Code.Should().Be("INSUFFICIENT_PERMISSIONS");
    }

    [Fact]
    public async Task UserWithNoRole_CannotAssignRoles_Returns403()
    {
        var callerId = await RegisterUserAsync("rbac-norole");
        var targetId = await CreateUserInDatabaseAsync("rbac-target3@example.com");
        var eventId = 402;
        // No role seeded for caller

        var request = new Contracts.Events.AssignRoleRequest(targetId, "Staff");
        using var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/roles", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var problem = await response.Content.ReadFromJsonAsync<ApiProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Code.Should().Be("INSUFFICIENT_PERMISSIONS");
    }

    [Fact]
    public async Task Unauthenticated_Returns401()
    {
        using var client = fixture.Factory.CreateClient();
        var request = new Contracts.Events.AssignRoleRequest(Guid.NewGuid(), "Staff");
        using var response = await client.PostAsJsonAsync("/api/events/1/roles", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PerEventIsolation_OwnerOnEventA_StaffOnEventB()
    {
        var callerId = await RegisterUserAsync("rbac-isolation");
        var targetId = await CreateUserInDatabaseAsync("rbac-target4@example.com");
        var eventA = 403;
        var eventB = 404;
        await SeedOwnerRoleAsync(eventA, callerId);
        await SeedRoleAsync(eventB, callerId, EventRole.Staff);

        // Owner on Event A — can assign roles
        var requestA = new Contracts.Events.AssignRoleRequest(targetId, "Staff");
        using var responseA = await _client.PostAsJsonAsync($"/api/events/{eventA}/roles", requestA);
        responseA.StatusCode.Should().Be(HttpStatusCode.Created);

        // Staff on Event B — cannot assign roles
        var requestB = new Contracts.Events.AssignRoleRequest(targetId, "Staff");
        using var responseB = await _client.PostAsJsonAsync($"/api/events/{eventB}/roles", requestB);
        responseB.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Owner_CanRevokeRoles()
    {
        var callerId = await RegisterUserAsync("rbac-owner-revoke");
        var targetId = await CreateUserInDatabaseAsync("rbac-target5@example.com");
        var eventId = 405;
        await SeedOwnerRoleAsync(eventId, callerId);
        await SeedRoleAsync(eventId, targetId, EventRole.Staff);

        using var response = await _client.DeleteAsync($"/api/events/{eventId}/roles/{targetId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Staff_CannotRevokeRoles_Returns403()
    {
        var callerId = await RegisterUserAsync("rbac-staff-revoke");
        var targetId = await CreateUserInDatabaseAsync("rbac-target6@example.com");
        var eventId = 406;
        await SeedRoleAsync(eventId, callerId, EventRole.Staff);
        await SeedRoleAsync(eventId, targetId, EventRole.Staff);

        using var response = await _client.DeleteAsync($"/api/events/{eventId}/roles/{targetId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var problem = await response.Content.ReadFromJsonAsync<ApiProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Code.Should().Be("INSUFFICIENT_PERMISSIONS");
    }

    [Fact]
    public async Task Owner_CanListRoleAssignments()
    {
        var callerId = await RegisterUserAsync("rbac-owner-list");
        var eventId = 407;
        await SeedOwnerRoleAsync(eventId, callerId);

        using var response = await _client.GetAsync($"/api/events/{eventId}/roles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Staff_CannotListRoleAssignments_Returns403()
    {
        var callerId = await RegisterUserAsync("rbac-staff-list");
        var eventId = 408;
        await SeedRoleAsync(eventId, callerId, EventRole.Staff);

        using var response = await _client.GetAsync($"/api/events/{eventId}/roles");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var problem = await response.Content.ReadFromJsonAsync<ApiProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Code.Should().Be("INSUFFICIENT_PERMISSIONS");
    }

    private async Task<Guid> RegisterUserAsync(string suffix)
    {
        var request = new RegisterUserRequest(
            $"User {suffix}",
            $"{suffix}_{Guid.NewGuid():N}@example.com",
            "SecurePass1!");

        using var response = await _client.PostAsJsonAsync("/api/users", request);
        response.EnsureSuccessStatusCode();
        var registration = await response.Content.ReadFromJsonAsync<UserRegistrationResponse>();
        return registration!.UserId;
    }

    private async Task<Guid> CreateUserInDatabaseAsync(string email)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        var userId = Guid.NewGuid();
        databaseContext.Users.Add(new UserRecord
        {
            Id = userId,
            DisplayName = "Test User",
            Email = email,
            PasswordHash = "hashed-password-stub",
            Role = UserRole.Organizer,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        });

        await databaseContext.SaveChangesAsync();
        return userId;
    }

    private async Task SeedOwnerRoleAsync(int eventId, Guid userId)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        databaseContext.EventUserRoles.Add(new EventUserRoleRecord
        {
            EventId = eventId,
            UserId = userId,
            Role = EventRole.Owner,
            CreatedAt = DateTimeOffset.UtcNow,
        });

        await databaseContext.SaveChangesAsync();
    }

    private async Task SeedRoleAsync(int eventId, Guid userId, EventRole role)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        databaseContext.EventUserRoles.Add(new EventUserRoleRecord
        {
            EventId = eventId,
            UserId = userId,
            Role = role,
            CreatedAt = DateTimeOffset.UtcNow,
        });

        await databaseContext.SaveChangesAsync();
    }
}
