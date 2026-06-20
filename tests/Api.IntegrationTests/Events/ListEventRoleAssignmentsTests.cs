using System.Net;
using System.Net.Http.Json;
using EventHub.Api.IntegrationTests.Integration;
using EventHub.Contracts.Events;
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
public sealed class ListEventRoleAssignmentsTests(IntegrationTestFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient(
        new WebApplicationFactoryClientOptions { HandleCookies = true });

    [Fact]
    public async Task ListAssignments_ReturnsAllAssignmentsWithUserDetails()
    {
        var callerId = await RegisterUserAsync("owner-list");
        var staffId = await CreateUserInDatabaseAsync("staff-list@example.com");
        var eventId = 300;
        await SeedOwnerRoleAsync(eventId, callerId);
        await SeedRoleAsync(eventId, staffId, EventRole.Staff);

        using var response = await _client.GetAsync($"/api/events/{eventId}/roles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var assignments = await response.Content.ReadFromJsonAsync<List<EventRoleAssignmentResponse>>();
        assignments.Should().NotBeNull();
        assignments.Should().HaveCount(2);
        assignments.Should().Contain(assignment =>
            assignment.UserId == callerId && assignment.Role == "Owner");
        assignments.Should().Contain(assignment =>
            assignment.UserId == staffId && assignment.Role == "Staff");
    }

    [Fact]
    public async Task ListAssignments_NonOwnerCaller_Returns403()
    {
        var callerId = await RegisterUserAsync("staff-listforbidden");
        var eventId = 301;
        await SeedRoleAsync(eventId, callerId, EventRole.Staff);

        using var response = await _client.GetAsync($"/api/events/{eventId}/roles");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ListAssignments_WhenNotAuthenticated_Returns401()
    {
        using var client = fixture.Factory.CreateClient();
        using var response = await client.GetAsync("/api/events/1/roles");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListAssignments_EmptyEvent_ReturnsEmptyList()
    {
        var callerId = await RegisterUserAsync("owner-empty");
        var eventId = 302;
        await SeedOwnerRoleAsync(eventId, callerId);

        using var response = await _client.GetAsync($"/api/events/{eventId}/roles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var assignments = await response.Content.ReadFromJsonAsync<List<EventRoleAssignmentResponse>>();
        assignments.Should().NotBeNull();
        assignments.Should().ContainSingle();
        assignments.Should().Contain(assignment => assignment.UserId == callerId && assignment.Role == "Owner");
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
