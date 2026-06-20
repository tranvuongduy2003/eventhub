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
public sealed class RevokeInvitationTests(IntegrationTestFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient(
        new WebApplicationFactoryClientOptions { HandleCookies = true });

    [Fact]
    public async Task OwnerRevokesInvitation_Returns204()
    {
        var callerId = await RegisterUserAsync("owner-revoke");
        var eventId = 400;
        await SeedOwnerRoleAsync(eventId, callerId);

        var invitationId = await CreateInvitationAsync(eventId, "revoke-target@example.com");

        using var response = await _client.DeleteAsync(
            $"/api/events/{eventId}/invitations/{invitationId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task NonOwnerRevokesInvitation_Returns403()
    {
        // Owner creates the invitation
        using var ownerClient = fixture.Factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = true });
        var ownerGuid = await RegisterUserAsync(ownerClient, "owner-for-revoke");
        var eventId = 401;
        await SeedOwnerRoleAsync(eventId, ownerGuid);

        var invitationId = await CreateInvitationAsync(ownerClient, eventId, "revoke-staff@example.com");

        // Staff user tries to revoke
        using var staffClient = fixture.Factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = true });
        var staffGuid = await RegisterUserAsync(staffClient, "staff-revoke");
        await SeedRoleAsync(eventId, staffGuid, EventRole.Staff);

        using var response = await staffClient.DeleteAsync(
            $"/api/events/{eventId}/invitations/{invitationId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RevokeAlreadyAcceptedInvitation_Returns422()
    {
        var callerId = await RegisterUserAsync("owner-revoke-accepted");
        var eventId = 402;
        await SeedOwnerRoleAsync(eventId, callerId);

        var invitationId = await CreateInvitationAsync(eventId, "accepted-target@example.com");

        await MarkInvitationAcceptedAsync(invitationId);

        using var response = await _client.DeleteAsync(
            $"/api/events/{eventId}/invitations/{invitationId}");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
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

    private static async Task<Guid> RegisterUserAsync(HttpClient client, string suffix)
    {
        var request = new RegisterUserRequest(
            $"User {suffix}",
            $"{suffix}_{Guid.NewGuid():N}@example.com",
            "SecurePass1!");

        using var response = await client.PostAsJsonAsync("/api/users", request);
        response.EnsureSuccessStatusCode();
        var registration = await response.Content.ReadFromJsonAsync<UserRegistrationResponse>();
        return registration!.UserId;
    }

    private async Task<int> CreateInvitationAsync(int eventId, string email)
    {
        var sendRequest = new SendInvitationRequest(email, null);
        using var sendResponse = await _client.PostAsJsonAsync($"/api/events/{eventId}/invitations", sendRequest);
        sendResponse.EnsureSuccessStatusCode();
        var invitation = await sendResponse.Content.ReadFromJsonAsync<InvitationResponse>();
        return invitation!.Id;
    }

    private static async Task<int> CreateInvitationAsync(HttpClient client, int eventId, string email)
    {
        var sendRequest = new SendInvitationRequest(email, null);
        using var sendResponse = await client.PostAsJsonAsync($"/api/events/{eventId}/invitations", sendRequest);
        sendResponse.EnsureSuccessStatusCode();
        var invitation = await sendResponse.Content.ReadFromJsonAsync<InvitationResponse>();
        return invitation!.Id;
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

    private async Task MarkInvitationAcceptedAsync(int invitationId)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        var record = await databaseContext.EventInvitations
            .AsTracking()
            .SingleAsync(i => i.Id == invitationId);

        record.Status = InvitationStatus.Accepted;
        record.AcceptedAt = DateTimeOffset.UtcNow;
        await databaseContext.SaveChangesAsync();
    }
}
