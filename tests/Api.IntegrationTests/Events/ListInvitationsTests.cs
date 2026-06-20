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
public sealed class ListInvitationsTests(IntegrationTestFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient(
        new WebApplicationFactoryClientOptions { HandleCookies = true });

    [Fact]
    public async Task OwnerListsInvitations_Returns200_WithArray()
    {
        var callerId = await RegisterUserAsync("owner-list");
        var eventId = 500;
        await SeedOwnerRoleAsync(eventId, callerId);

        await CreateInvitationAsync(eventId, "list1@example.com");
        await CreateInvitationAsync(eventId, "list2@example.com");

        using var response = await _client.GetAsync($"/api/events/{eventId}/invitations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var invitations = await response.Content.ReadFromJsonAsync<List<InvitationResponse>>();
        invitations.Should().NotBeNull();
        invitations.Should().HaveCount(2);
    }

    [Fact]
    public async Task NonOwnerListsInvitations_Returns403()
    {
        var callerId = await RegisterUserAsync("staff-list");
        var eventId = 501;
        await SeedRoleAsync(eventId, callerId, EventRole.Staff);

        using var response = await _client.GetAsync($"/api/events/{eventId}/invitations");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
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

    private async Task CreateInvitationAsync(int eventId, string email)
    {
        var sendRequest = new SendInvitationRequest(email, null);
        using var sendResponse = await _client.PostAsJsonAsync($"/api/events/{eventId}/invitations", sendRequest);
        sendResponse.EnsureSuccessStatusCode();
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
