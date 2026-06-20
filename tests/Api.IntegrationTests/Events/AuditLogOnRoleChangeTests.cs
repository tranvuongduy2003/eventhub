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
public sealed class AuditLogOnRoleChangeTests(IntegrationTestFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient(
        new WebApplicationFactoryClientOptions { HandleCookies = true });

    [Fact]
    public async Task AssignRole_CreatesAuditEntry()
    {
        var callerId = await RegisterUserAsync("audit-assign-owner");
        var targetId = await CreateUserInDatabaseAsync("audit-assign-target@example.com");
        var eventId = 300;
        await SeedOwnerRoleAsync(eventId, callerId);

        var request = new AssignRoleRequest(targetId, "Staff");
        using var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/roles", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        var entries = await databaseContext.PermissionAuditEntries
            .AsNoTracking()
            .Where(e => e.EventId == eventId)
            .ToListAsync();

        entries.Should().ContainSingle();
        entries[0].Action.Should().Be(AuditAction.Assigned);
        entries[0].ActorId.Should().Be(callerId);
        entries[0].TargetId.Should().Be(targetId);
        entries[0].NewRole.Should().Be(EventRole.Staff);
        entries[0].OldRole.Should().BeNull();
    }

    [Fact]
    public async Task RevokeRole_CreatesAuditEntry()
    {
        var callerId = await RegisterUserAsync("audit-revoke-owner");
        var targetId = await CreateUserInDatabaseAsync("audit-revoke-target@example.com");
        var eventId = 301;
        await SeedOwnerRoleAsync(eventId, callerId);
        await SeedRoleAsync(eventId, targetId, EventRole.Staff);

        using var response = await _client.DeleteAsync($"/api/events/{eventId}/roles/{targetId}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        var entries = await databaseContext.PermissionAuditEntries
            .AsNoTracking()
            .Where(e => e.EventId == eventId && e.Action == AuditAction.Revoked)
            .ToListAsync();

        entries.Should().ContainSingle();
        entries[0].ActorId.Should().Be(callerId);
        entries[0].TargetId.Should().Be(targetId);
        entries[0].OldRole.Should().Be(EventRole.Staff);
        entries[0].NewRole.Should().BeNull();
    }

    [Fact]
    public async Task OwnershipTransfer_CreatesTwoAuditEntries()
    {
        var callerId = await RegisterUserAsync("audit-transfer-owner");
        var targetId = await CreateUserInDatabaseAsync("audit-transfer-target@example.com");
        var eventId = 302;
        await SeedOwnerRoleAsync(eventId, callerId);

        var request = new AssignRoleRequest(targetId, "Owner");
        using var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/roles", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        var entries = await databaseContext.PermissionAuditEntries
            .AsNoTracking()
            .Where(e => e.EventId == eventId)
            .OrderBy(e => e.Action)
            .ToListAsync();

        entries.Should().HaveCount(2);

        // First: Transferred entry for demoted old owner
        var transferred = entries.Should().ContainSingle(e => e.Action == AuditAction.Transferred).Subject;
        transferred.ActorId.Should().Be(callerId);
        transferred.OldRole.Should().Be(EventRole.Owner);
        transferred.NewRole.Should().Be(EventRole.Staff);

        // Second: Assigned entry for new owner
        var assigned = entries.Should().ContainSingle(e => e.Action == AuditAction.Assigned).Subject;
        assigned.ActorId.Should().Be(callerId);
        assigned.TargetId.Should().Be(targetId);
        assigned.OldRole.Should().BeNull();
        assigned.NewRole.Should().Be(EventRole.Owner);
    }

    [Fact]
    public async Task FailedAssignment_DoesNotCreateAuditEntry()
    {
        var callerId = await RegisterUserAsync("audit-fail-owner");
        var targetId = await CreateUserInDatabaseAsync("audit-fail-target@example.com");
        var eventId = 303;
        await SeedOwnerRoleAsync(eventId, callerId);
        await SeedRoleAsync(eventId, targetId, EventRole.Staff);

        // Assign Staff again — should fail with 409 (duplicate)
        var request = new AssignRoleRequest(targetId, "Staff");
        using var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/roles", request);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        var entries = await databaseContext.PermissionAuditEntries
            .AsNoTracking()
            .Where(e => e.EventId == eventId)
            .ToListAsync();

        entries.Should().BeEmpty();
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
