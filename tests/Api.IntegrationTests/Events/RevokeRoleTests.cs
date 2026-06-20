using System.Net;
using System.Net.Http.Json;
using EventHub.Api.IntegrationTests.Integration;
using EventHub.Contracts.Users;
using EventHub.Domain.Events;
using EventHub.Domain.Users;
using EventHub.Infrastructure.Persistence;
using EventHub.Infrastructure.Persistence.Entities;
using EventHub.Testing.Common.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.Api.IntegrationTests.Events;

[Collection(IntegrationTestCollection.Name)]
public sealed class RevokeRoleTests(IntegrationTestFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient(
        new WebApplicationFactoryClientOptions { HandleCookies = true });

    [Fact]
    public async Task RevokeStaffRole_Returns204_AndRemovesAssignment()
    {
        var callerId = await RegisterUserAsync("owner-revoke");
        var targetId = await CreateUserInDatabaseAsync("staff-revoke@example.com");
        var eventId = 200;
        await SeedOwnerRoleAsync(eventId, callerId);
        await SeedRoleAsync(eventId, targetId, EventRole.Staff);

        using var response = await _client.DeleteAsync($"/api/events/{eventId}/roles/{targetId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        var deleted = await databaseContext.EventUserRoles
            .AsNoTracking()
            .FirstOrDefaultAsync(eventUserRole =>
                eventUserRole.EventId == eventId && eventUserRole.UserId == targetId);

        deleted.Should().BeNull();
    }

    [Fact]
    public async Task RevokeOwnerRole_Returns422()
    {
        var callerId = await RegisterUserAsync("owner-selfrevoke");
        var eventId = 201;
        await SeedOwnerRoleAsync(eventId, callerId);

        using var response = await _client.DeleteAsync($"/api/events/{eventId}/roles/{callerId}");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task RevokeRole_NonOwnerCaller_Returns403()
    {
        var callerId = await RegisterUserAsync("staff-revokeforbidden");
        var targetId = await CreateUserInDatabaseAsync("target-revoke@example.com");
        var eventId = 202;
        await SeedRoleAsync(eventId, callerId, EventRole.Staff);
        await SeedRoleAsync(eventId, targetId, EventRole.Staff);

        using var response = await _client.DeleteAsync($"/api/events/{eventId}/roles/{targetId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RevokeRole_UserWithNoRole_Returns204()
    {
        var callerId = await RegisterUserAsync("owner-noop");
        var targetId = await CreateUserInDatabaseAsync("target-noop@example.com");
        var eventId = 203;
        await SeedOwnerRoleAsync(eventId, callerId);

        using var response = await _client.DeleteAsync($"/api/events/{eventId}/roles/{targetId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RevokeRole_WhenNotAuthenticated_Returns401()
    {
        using var client = fixture.Factory.CreateClient();
        using var response = await client.DeleteAsync($"/api/events/1/roles/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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
