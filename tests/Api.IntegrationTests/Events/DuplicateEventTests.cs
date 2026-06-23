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
public sealed class DuplicateEventTests(IntegrationTestFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient(
        new WebApplicationFactoryClientOptions { HandleCookies = true });

    [Fact]
    public async Task DuplicateEvent_OwnerOfDraftEvent_Returns201()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId);

        using var response = await _client.PostAsync($"/api/events/{eventId}/duplicate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<DuplicateEventResponse>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("Draft");
    }

    [Fact]
    public async Task DuplicateEvent_OwnerOfPublishedEvent_Returns201()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedPublishedEventAsync(userId);

        using var response = await _client.PostAsync($"/api/events/{eventId}/duplicate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<DuplicateEventResponse>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("Draft");
    }

    [Fact]
    public async Task DuplicateEvent_OwnerOfClosedEvent_Returns201()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedClosedEventAsync(userId);

        using var response = await _client.PostAsync($"/api/events/{eventId}/duplicate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<DuplicateEventResponse>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("Draft");
    }

    [Fact]
    public async Task DuplicateEvent_OwnerOfCancelledEvent_Returns201()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedCancelledEventAsync(userId);

        using var response = await _client.PostAsync($"/api/events/{eventId}/duplicate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<DuplicateEventResponse>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("Draft");
    }

    [Fact]
    public async Task DuplicateEvent_NewEventHasCopiedDetails()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId);

        using var duplicateResponse = await _client.PostAsync($"/api/events/{eventId}/duplicate", null);
        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var newEventId = await FindDuplicatedEventIdAsync(eventId);

        using var getResponse = await _client.GetAsync($"/api/events/{newEventId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var details = await getResponse.Content.ReadFromJsonAsync<EventDetailsResponse>();
        details.Should().NotBeNull();
        details!.Title.Should().StartWith("Copy of ");
        details.Description.Should().NotBeNull();
        details.PhysicalAddress.Should().NotBeNull();
    }

    [Fact]
    public async Task DuplicateEvent_NewEventHasNullSchedule()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId);

        using var duplicateResponse = await _client.PostAsync($"/api/events/{eventId}/duplicate", null);
        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var newEventId = await FindDuplicatedEventIdAsync(eventId);

        using var getResponse = await _client.GetAsync($"/api/events/{newEventId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var details = await getResponse.Content.ReadFromJsonAsync<EventDetailsResponse>();
        details.Should().NotBeNull();
        details!.StartsAt.Should().BeNull();
        details.EndsAt.Should().BeNull();
        details.TimeZoneId.Should().BeNull();
    }

    [Fact]
    public async Task DuplicateEvent_NewEventHasNoSlug()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedPublishedEventAsync(userId);

        using var duplicateResponse = await _client.PostAsync($"/api/events/{eventId}/duplicate", null);
        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var newEventId = await FindDuplicatedEventIdAsync(eventId);

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var newEvent = databaseContext.Events.Single(e => e.Id == newEventId);
        newEvent.Slug.Should().BeNull();
    }

    [Fact]
    public async Task DuplicateEvent_NewEventHasDraftStatus()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedPublishedEventAsync(userId);

        using var duplicateResponse = await _client.PostAsync($"/api/events/{eventId}/duplicate", null);
        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var duplicateResult = await duplicateResponse.Content.ReadFromJsonAsync<DuplicateEventResponse>();
        duplicateResult.Should().NotBeNull();
        duplicateResult!.Status.Should().Be("Draft");
    }

    [Fact]
    public async Task DuplicateEvent_NoAuth_Returns401()
    {
        using var response = await _client.PostAsync("/api/events/1/duplicate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DuplicateEvent_NonOwner_Returns403()
    {
        var ownerId = await RegisterOrganizerAsync();
        var eventId = await SeedPublishedEventAsync(ownerId);

        await RegisterOrganizerAsync();

        using var response = await _client.PostAsync($"/api/events/{eventId}/duplicate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DuplicateEvent_NonExistentEvent_Returns403()
    {
        await RegisterOrganizerAsync();

        using var response = await _client.PostAsync("/api/events/99999/duplicate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
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

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var user = databaseContext.Users.OrderByDescending(u => u.CreatedAt).First();
        return user.Id;
    }

    private async Task<int> FindDuplicatedEventIdAsync(int sourceEventId)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var sourceEvent = databaseContext.Events.Single(e => e.Id == sourceEventId);
        var duplicatedEvent = databaseContext.Events
            .Where(e => e.Title.StartsWith($"Copy of {sourceEvent.Title}"))
            .OrderByDescending(e => e.CreatedAt)
            .First();
        return duplicatedEvent.Id;
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
            Description = "A great tech conference",
            CoverImageKey = "covers/test.jpg",
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

    private async Task<int> SeedPublishedEventAsync(Guid organizerId)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var eventRecord = new EventRecord
        {
            Title = $"Published Conference {suffix}",
            OrganizerId = organizerId,
            ScheduleStartsAt = new DateTimeOffset(2026, 7, 15, 14, 0, 0, TimeSpan.Zero),
            ScheduleEndsAt = new DateTimeOffset(2026, 7, 15, 16, 0, 0, TimeSpan.Zero),
            ScheduleTimeZoneId = "UTC",
            LocationPhysicalAddress = "123 Conference Ave",
            LocationIsOnline = false,
            Status = EventStatus.Published,
            Slug = $"published-conf-{suffix}",
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

    private async Task<int> SeedClosedEventAsync(Guid organizerId)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var eventRecord = new EventRecord
        {
            Title = $"Closed Conference {suffix}",
            OrganizerId = organizerId,
            ScheduleStartsAt = new DateTimeOffset(2026, 7, 15, 14, 0, 0, TimeSpan.Zero),
            ScheduleEndsAt = new DateTimeOffset(2026, 7, 15, 16, 0, 0, TimeSpan.Zero),
            ScheduleTimeZoneId = "UTC",
            LocationPhysicalAddress = "123 Conference Ave",
            LocationIsOnline = false,
            Status = EventStatus.Closed,
            Slug = $"closed-conf-{suffix}",
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

    private async Task<int> SeedCancelledEventAsync(Guid organizerId)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var eventRecord = new EventRecord
        {
            Title = $"Cancelled Conference {suffix}",
            OrganizerId = organizerId,
            ScheduleStartsAt = new DateTimeOffset(2026, 7, 15, 14, 0, 0, TimeSpan.Zero),
            ScheduleEndsAt = new DateTimeOffset(2026, 7, 15, 16, 0, 0, TimeSpan.Zero),
            ScheduleTimeZoneId = "UTC",
            LocationPhysicalAddress = "123 Conference Ave",
            LocationIsOnline = false,
            Status = EventStatus.Cancelled,
            CancelledAt = DateTimeOffset.UtcNow,
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
