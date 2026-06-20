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
public sealed class ListAuditLogTests(IntegrationTestFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient(
        new WebApplicationFactoryClientOptions { HandleCookies = true });

    [Fact]
    public async Task ListAuditLog_OwnerCanView_Returns200()
    {
        var callerId = await RegisterUserAsync("audit-owner");
        var eventId = 200;
        await SeedOwnerRoleAsync(eventId, callerId);

        using var response = await _client.GetAsync($"/api/events/{eventId}/audit-log");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AuditLogResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNull();
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task ListAuditLog_StaffCannotView_Returns403()
    {
        var callerId = await RegisterUserAsync("audit-staff");
        var eventId = 201;
        await SeedRoleAsync(eventId, callerId, EventRole.Staff);

        using var response = await _client.GetAsync($"/api/events/{eventId}/audit-log");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ListAuditLog_Unauthenticated_Returns401()
    {
        using var client = fixture.Factory.CreateClient();
        using var response = await client.GetAsync("/api/events/1/audit-log");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListAuditLog_EmptyLog_ReturnsEmptyList()
    {
        var callerId = await RegisterUserAsync("audit-empty");
        var eventId = 202;
        await SeedOwnerRoleAsync(eventId, callerId);

        using var response = await _client.GetAsync($"/api/events/{eventId}/audit-log");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AuditLogResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task ListAuditLog_Pagination_RespectsPageSize()
    {
        var callerId = await RegisterUserAsync("audit-page");
        var targetId = await CreateUserInDatabaseAsync("audit-target-page@example.com");
        var eventId = 203;
        await SeedOwnerRoleAsync(eventId, callerId);

        // Create 3 audit entries by assigning and revoking roles
        await SeedAuditEntryAsync(eventId, callerId, targetId, AuditAction.Assigned, null, EventRole.Staff);
        await SeedAuditEntryAsync(eventId, callerId, targetId, AuditAction.Revoked, EventRole.Staff, null);
        await SeedAuditEntryAsync(eventId, callerId, targetId, AuditAction.Assigned, null, EventRole.Owner);

        using var response = await _client.GetAsync($"/api/events/{eventId}/audit-log?page=1&pageSize=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AuditLogResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(3);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
    }

    [Fact]
    public async Task ListAuditLog_ActionFilter_ReturnsMatchingEntries()
    {
        var callerId = await RegisterUserAsync("audit-action-filter");
        var targetId = await CreateUserInDatabaseAsync("audit-target-action@example.com");
        var eventId = 204;
        await SeedOwnerRoleAsync(eventId, callerId);

        await SeedAuditEntryAsync(eventId, callerId, targetId, AuditAction.Assigned, null, EventRole.Staff);
        await SeedAuditEntryAsync(eventId, callerId, targetId, AuditAction.Revoked, EventRole.Staff, null);

        using var response = await _client.GetAsync($"/api/events/{eventId}/audit-log?action=assigned");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AuditLogResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items[0].Action.Should().Be("Assigned");
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

    private async Task SeedAuditEntryAsync(
        int eventId, Guid actorId, Guid targetId,
        AuditAction action, EventRole? oldRole, EventRole? newRole)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        databaseContext.PermissionAuditEntries.Add(new PermissionAuditEntryRecord
        {
            EventId = eventId,
            ActorId = actorId,
            TargetId = targetId,
            Action = action,
            OldRole = oldRole,
            NewRole = newRole,
            OccurredAt = DateTimeOffset.UtcNow,
        });

        await databaseContext.SaveChangesAsync();
    }
}
