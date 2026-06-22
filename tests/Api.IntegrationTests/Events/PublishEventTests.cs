using System.Net;
using System.Net.Http.Json;
using EventHub.Api.IntegrationTests.Integration;
using EventHub.Contracts.Events;
using EventHub.Contracts.Users;
using EventHub.Domain.Events;
using EventHub.Infrastructure.Persistence;
using EventHub.Infrastructure.Persistence.Entities;
using EventHub.Testing.Common.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.Api.IntegrationTests.Events;

[Collection(IntegrationTestCollection.Name)]
public sealed class PublishEventTests(IntegrationTestFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient(
        new WebApplicationFactoryClientOptions { HandleCookies = true });

    [Fact]
    public async Task PublishEvent_DraftEvent_Returns200WithPublishedStatus()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId);

        using var response = await _client.PostAsync($"/api/events/{eventId}/publish", null);

        var publishResult = await PublishEventTestHelpers.AssertPublishedAsync(response);
        publishResult.Slug.Should().Contain("-");
    }

    [Fact]
    public async Task PublishEvent_AlreadyPublished_Returns422()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId);

        using var firstPublish = await _client.PostAsync($"/api/events/{eventId}/publish", null);
        firstPublish.StatusCode.Should().Be(HttpStatusCode.OK);

        using var secondPublish = await _client.PostAsync($"/api/events/{eventId}/publish", null);

        await PublishEventTestHelpers.AssertAlreadyPublishedAsync(secondPublish);
    }

    [Fact]
    public async Task PublishEvent_NonExistentEvent_Returns404()
    {
        await RegisterOrganizerAsync();

        using var response = await _client.PostAsync("/api/events/99999/publish", null);

        await PublishEventTestHelpers.AssertNotFoundAsync(response);
    }

    [Fact]
    public async Task PublishEvent_NoAuth_Returns401()
    {
        using var response = await _client.PostAsync("/api/events/1/publish", null);

        await PublishEventTestHelpers.AssertUnauthorizedAsync(response);
    }

    [Fact]
    public async Task PublishEvent_PublishedEventDetailsShowPublishedStatus()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId);

        using var publishResponse = await _client.PostAsync($"/api/events/{eventId}/publish", null);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var getResponse = await _client.GetAsync($"/api/events/{eventId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var details = await getResponse.Content.ReadFromJsonAsync<EventDetailsResponse>();
        details.Should().NotBeNull();
        details!.Status.Should().Be("Published");
    }

    private async Task<Guid> RegisterOrganizerAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var request = new RegisterUserRequest(
            $"Organizer {suffix}",
            $"organizer_{suffix}@example.com",
            "SecurePass1!");

        using var response = await _client.PostAsJsonAsync("/api/users", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var userId = Guid.NewGuid();
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var user = databaseContext.Users.OrderByDescending(u => u.CreatedAt).First();
        return user.Id;
    }

    private async Task<int> SeedDraftEventAsync(Guid organizerId)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var eventRecord = new EventRecord
        {
            Title = $"Tech Conference {suffix}",
            OrganizerId = organizerId,
            ScheduleStartsAt = new DateTimeOffset(2026, 7, 15, 14, 0, 0, TimeSpan.Zero),
            ScheduleEndsAt = new DateTimeOffset(2026, 7, 15, 16, 0, 0, TimeSpan.Zero),
            ScheduleTimeZoneId = "UTC",
            LocationPhysicalAddress = "123 Conference Ave",
            LocationIsOnline = false,
            Status = EventStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        databaseContext.Events.Add(eventRecord);
        await databaseContext.SaveChangesAsync();

        databaseContext.EventUserRoles.Add(new EventUserRoleRecord
        {
            EventId = eventRecord.Id,
            UserId = organizerId,
            Role = EventRole.Owner,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await databaseContext.SaveChangesAsync();

        return eventRecord.Id;
    }
}
