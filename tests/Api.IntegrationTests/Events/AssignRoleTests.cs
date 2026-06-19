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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.Api.IntegrationTests.Events;

[Collection(IntegrationTestCollection.Name)]
public sealed class AssignRoleTests(IntegrationTestFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient(
        new WebApplicationFactoryClientOptions { HandleCookies = true });

    [Fact]
    public async Task AssignStaffRole_Returns201_WithAssignmentDetails()
    {
        var callerId = await RegisterUserAsync("owner");
        var targetId = await CreateUserInDatabaseAsync("target@example.com");
        var eventId = 100;
        await SeedOwnerRoleAsync(eventId, callerId);

        var request = new AssignRoleRequest(targetId, "Staff");
        using var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/roles", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<EventRoleAssignmentResponse>();
        result.Should().NotBeNull();
        result!.UserId.Should().Be(targetId);
        result.Role.Should().Be("Staff");
        result.Email.Should().Be("target@example.com");
    }

    [Fact]
    public async Task AssignOwnerRole_TransfersOwnership_DemotesPreviousOwnerToStaff()
    {
        var callerId = await RegisterUserAsync("original-owner");
        var targetId = await CreateUserInDatabaseAsync("new-owner@example.com");
        var eventId = 101;
        await SeedOwnerRoleAsync(eventId, callerId);

        var request = new AssignRoleRequest(targetId, "Owner");
        using var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/roles", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<EventRoleAssignmentResponse>();
        result.Should().NotBeNull();
        result!.UserId.Should().Be(targetId);
        result.Role.Should().Be("Owner");

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        var previousOwnerRole = await databaseContext.EventUserRoles
            .AsNoTracking()
            .SingleAsync(eventUserRole =>
                eventUserRole.EventId == eventId && eventUserRole.UserId == callerId);
        previousOwnerRole.Role.Should().Be(EventRole.Staff);

        var newOwnerRole = await databaseContext.EventUserRoles
            .AsNoTracking()
            .SingleAsync(eventUserRole =>
                eventUserRole.EventId == eventId && eventUserRole.UserId == targetId);
        newOwnerRole.Role.Should().Be(EventRole.Owner);
    }

    [Fact]
    public async Task AssignDuplicateRole_Returns409()
    {
        var callerId = await RegisterUserAsync("owner-dup");
        var targetId = await CreateUserInDatabaseAsync("staff-dup@example.com");
        var eventId = 102;
        await SeedOwnerRoleAsync(eventId, callerId);
        await SeedRoleAsync(eventId, targetId, EventRole.Staff);

        var request = new AssignRoleRequest(targetId, "Staff");
        using var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/roles", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AssignRole_NonOwnerCaller_Returns403()
    {
        var callerId = await RegisterUserAsync("staff-caller");
        var targetId = await CreateUserInDatabaseAsync("target-forbidden@example.com");
        var eventId = 103;
        await SeedRoleAsync(eventId, callerId, EventRole.Staff);

        var request = new AssignRoleRequest(targetId, "Staff");
        using var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/roles", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AssignRole_UserNotFound_Returns404()
    {
        var callerId = await RegisterUserAsync("owner-notfound");
        var eventId = 104;
        await SeedOwnerRoleAsync(eventId, callerId);

        var request = new AssignRoleRequest(Guid.NewGuid(), "Staff");
        using var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/roles", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AssignRole_SelfAssignment_Returns422()
    {
        var callerId = await RegisterUserAsync("owner-self");
        var eventId = 105;
        await SeedOwnerRoleAsync(eventId, callerId);

        var request = new AssignRoleRequest(callerId, "Staff");
        using var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/roles", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task AssignRole_InvalidRole_Returns422()
    {
        var callerId = await RegisterUserAsync("owner-invalidrole");
        var targetId = await CreateUserInDatabaseAsync("target-invalidrole@example.com");
        var eventId = 106;
        await SeedOwnerRoleAsync(eventId, callerId);

        var request = new AssignRoleRequest(targetId, "SuperAdmin");
        using var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/roles", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task AssignRole_WhenNotAuthenticated_Returns401()
    {
        using var client = fixture.Factory.CreateClient();
        var request = new AssignRoleRequest(Guid.NewGuid(), "Staff");
        using var response = await client.PostAsJsonAsync("/api/events/1/roles", request);

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
