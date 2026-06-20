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
public sealed class AcceptInvitationTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task AcceptWithValidToken_Returns200_AndAssignsRole()
    {
        using var ownerClient = fixture.Factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = true });

        var ownerGuid = await RegisterUserAsync(ownerClient, "owner-accept");
        var eventId = 300;
        await SeedOwnerRoleAsync(eventId, ownerGuid);

        using var inviteeClient = fixture.Factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = true });
        var inviteeEmail = $"accept_ok_{Guid.NewGuid():N}@example.com";
        var inviteeGuid = await RegisterUserWithEmailAsync(inviteeClient, inviteeEmail);

        var sendRequest = new SendInvitationRequest(inviteeEmail, null);
        using var sendResponse = await ownerClient.PostAsJsonAsync($"/api/events/{eventId}/invitations", sendRequest);
        sendResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var invitation = await sendResponse.Content.ReadFromJsonAsync<InvitationResponse>();

        var token = invitation!.Token!;

        var acceptRequest = new AcceptInvitationRequest(token);
        using var acceptResponse = await inviteeClient.PostAsJsonAsync(
            $"/api/invitations/{invitation.Id}/accept", acceptRequest);

        acceptResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var role = await databaseContext.EventUserRoles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == inviteeGuid);
        role.Should().NotBeNull();
        role!.Role.Should().Be(EventRole.Staff);
    }

    [Fact]
    public async Task AcceptExpiredInvitation_Returns422()
    {
        using var ownerClient = fixture.Factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = true });

        var ownerGuid = await RegisterUserAsync(ownerClient, "owner-exp");
        var eventId = 301;
        await SeedOwnerRoleAsync(eventId, ownerGuid);

        using var inviteeClient = fixture.Factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = true });
        var inviteeEmail = $"accept_exp_{Guid.NewGuid():N}@example.com";
        await RegisterUserWithEmailAsync(inviteeClient, inviteeEmail);

        var sendRequest = new SendInvitationRequest(inviteeEmail, 1);
        using var sendResponse = await ownerClient.PostAsJsonAsync($"/api/events/{eventId}/invitations", sendRequest);
        sendResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var invitation = await sendResponse.Content.ReadFromJsonAsync<InvitationResponse>();

        await SetInvitationExpiredAsync(invitation!.Id);

        var token = invitation.Token!;

        var acceptRequest = new AcceptInvitationRequest(token);
        using var acceptResponse = await inviteeClient.PostAsJsonAsync(
            $"/api/invitations/{invitation.Id}/accept", acceptRequest);

        acceptResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task AcceptRevokedInvitation_Returns422()
    {
        using var ownerClient = fixture.Factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = true });

        var ownerGuid = await RegisterUserAsync(ownerClient, "owner-rev");
        var eventId = 302;
        await SeedOwnerRoleAsync(eventId, ownerGuid);

        using var inviteeClient = fixture.Factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = true });
        var inviteeEmail = $"accept_rev_{Guid.NewGuid():N}@example.com";
        await RegisterUserWithEmailAsync(inviteeClient, inviteeEmail);

        var sendRequest = new SendInvitationRequest(inviteeEmail, null);
        using var sendResponse = await ownerClient.PostAsJsonAsync($"/api/events/{eventId}/invitations", sendRequest);
        sendResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var invitation = await sendResponse.Content.ReadFromJsonAsync<InvitationResponse>();

        using var revokeResponse = await ownerClient.DeleteAsync(
            $"/api/events/{eventId}/invitations/{invitation!.Id}");
        revokeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var token = invitation.Token!;

        var acceptRequest = new AcceptInvitationRequest(token);
        using var acceptResponse = await inviteeClient.PostAsJsonAsync(
            $"/api/invitations/{invitation.Id}/accept", acceptRequest);

        acceptResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task AcceptWhenAlreadyHasRole_Returns409()
    {
        using var ownerClient = fixture.Factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = true });

        var ownerGuid = await RegisterUserAsync(ownerClient, "owner-ar");
        var eventId = 303;
        await SeedOwnerRoleAsync(eventId, ownerGuid);

        using var inviteeClient = fixture.Factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = true });
        var inviteeEmail = $"accept_ar_{Guid.NewGuid():N}@example.com";
        var inviteeGuid = await RegisterUserWithEmailAsync(inviteeClient, inviteeEmail);

        var sendRequest = new SendInvitationRequest(inviteeEmail, null);
        using var sendResponse = await ownerClient.PostAsJsonAsync($"/api/events/{eventId}/invitations", sendRequest);
        sendResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var invitation = await sendResponse.Content.ReadFromJsonAsync<InvitationResponse>();

        await SeedRoleAsync(eventId, inviteeGuid, EventRole.Staff);

        var token = invitation!.Token!;

        var acceptRequest = new AcceptInvitationRequest(token);
        using var acceptResponse = await inviteeClient.PostAsJsonAsync(
            $"/api/invitations/{invitation.Id}/accept", acceptRequest);

        acceptResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
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

    private static async Task<Guid> RegisterUserWithEmailAsync(HttpClient client, string email)
    {
        var request = new RegisterUserRequest("Test User", email, "SecurePass1!");

        using var response = await client.PostAsJsonAsync("/api/users", request);
        response.EnsureSuccessStatusCode();
        var registration = await response.Content.ReadFromJsonAsync<UserRegistrationResponse>();
        return registration!.UserId;
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

    private async Task SetInvitationExpiredAsync(int invitationId)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        var record = await databaseContext.EventInvitations
            .AsTracking()
            .SingleAsync(i => i.Id == invitationId);

        record.Status = InvitationStatus.Expired;
        await databaseContext.SaveChangesAsync();
    }
}
