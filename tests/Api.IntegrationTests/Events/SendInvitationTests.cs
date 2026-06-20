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
public sealed class SendInvitationTests(IntegrationTestFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient(
        new WebApplicationFactoryClientOptions { HandleCookies = true });

    [Fact]
    public async Task OwnerSendsInvitation_Returns201_WithInvitationDetails()
    {
        var callerId = await RegisterUserAsync("owner-send");
        var eventId = 200;
        await SeedOwnerRoleAsync(eventId, callerId);

        var request = new SendInvitationRequest("alice@example.com", null);
        using var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/invitations", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<InvitationResponse>();
        result.Should().NotBeNull();
        result!.Email.Should().Be("alice@example.com");
        result.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task NonOwnerSendsInvitation_Returns403()
    {
        var callerId = await RegisterUserAsync("staff-send");
        var eventId = 201;
        await SeedRoleAsync(eventId, callerId, EventRole.Staff);

        var request = new SendInvitationRequest("bob@example.com", null);
        using var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/invitations", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DuplicatePendingInvitation_Returns409()
    {
        var callerId = await RegisterUserAsync("owner-dup-invite");
        var eventId = 202;
        await SeedOwnerRoleAsync(eventId, callerId);

        var request = new SendInvitationRequest("dup@example.com", null);
        using var first = await _client.PostAsJsonAsync($"/api/events/{eventId}/invitations", request);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        using var second = await _client.PostAsJsonAsync($"/api/events/{eventId}/invitations", request);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task SelfInvite_Returns422()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"self_{suffix}@example.com";

        var registerRequest = new RegisterUserRequest($"Self User {suffix}", email, "SecurePass1!");
        using var registerResponse = await _client.PostAsJsonAsync("/api/users", registerRequest);
        registerResponse.EnsureSuccessStatusCode();
        var registration = await registerResponse.Content.ReadFromJsonAsync<UserRegistrationResponse>();

        var eventId = 203;
        await SeedOwnerRoleAsync(eventId, registration!.UserId);

        var inviteRequest = new SendInvitationRequest(email, null);
        using var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/invitations", inviteRequest);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task InviteeAlreadyHasRole_Returns409()
    {
        var callerId = await RegisterUserAsync("owner-role-exists");
        var targetId = await CreateUserInDatabaseAsync("existing-staff@example.com");
        var eventId = 204;
        await SeedOwnerRoleAsync(eventId, callerId);
        await SeedRoleAsync(eventId, targetId, EventRole.Staff);

        var request = new SendInvitationRequest("existing-staff@example.com", null);
        using var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/invitations", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task NotAuthenticated_Returns401()
    {
        using var client = fixture.Factory.CreateClient();
        var request = new SendInvitationRequest("anon@example.com", null);
        using var response = await client.PostAsJsonAsync("/api/events/1/invitations", request);

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
