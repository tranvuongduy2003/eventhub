using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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
public sealed class GetEventLocationsTests(IntegrationTestFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient(
        new WebApplicationFactoryClientOptions { HandleCookies = true });

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task GetEventLocations_ReturnsDistinctLocations()
    {
        var userId = await RegisterOrganizerAsync();

        var eventId1 = await SeedDraftEventAsync(userId, locationPhysicalAddress: "Ho Chi Minh City");
        await SeedTicketTypeAsync(eventId1, "General", 50m, 100);
        await PublishEventAsync(eventId1);

        var eventId2 = await SeedDraftEventAsync(userId, locationPhysicalAddress: "Hanoi");
        await SeedTicketTypeAsync(eventId2, "General", 30m, 100);
        await PublishEventAsync(eventId2);

        // Duplicate location
        var eventId3 = await SeedDraftEventAsync(userId, locationPhysicalAddress: "Ho Chi Minh City");
        await SeedTicketTypeAsync(eventId3, "General", 20m, 100);
        await PublishEventAsync(eventId3);

        using var response = await _client.GetAsync("/api/events/locations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Should().Contain("Ho Chi Minh City");
        result.Should().Contain("Hanoi");
        result.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task GetEventLocations_ExcludesDraftEvents()
    {
        var userId = await RegisterOrganizerAsync();

        // Draft only — should not appear
        await SeedDraftEventAsync(userId, locationPhysicalAddress: "Draft City");

        using var response = await _client.GetAsync("/api/events/locations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Should().NotContain("Draft City");
    }

    [Fact]
    public async Task GetEventLocations_IncludesOnlineOption()
    {
        var userId = await RegisterOrganizerAsync();

        var eventId = await SeedDraftEventAsync(
            userId,
            locationPhysicalAddress: null,
            locationIsOnline: true);
        await SeedTicketTypeAsync(eventId, "General", 0m, 100);
        await PublishEventAsync(eventId);

        using var response = await _client.GetAsync("/api/events/locations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Should().Contain("Online");
    }

    [Fact]
    public async Task GetEventLocations_AnonymousAccess_Returns200()
    {
        using var unauthenticatedClient = fixture.Factory.CreateClient();

        using var response = await unauthenticatedClient.GetAsync("/api/events/locations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<Guid> RegisterOrganizerAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var request = new RegisterUserRequest(
            $"Organizer_{suffix}",
            $"organizer_{suffix}@example.com",
            "SecurePass1!");

        using var response = await _client.PostAsJsonAsync("/api/users", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var user = databaseContext.Users.OrderByDescending(u => u.CreatedAt).First();
        return user.Id;
    }

    private async Task<int> SeedDraftEventAsync(
        Guid organizerId,
        DateTimeOffset? startsAt = null,
        string? title = null,
        string? locationPhysicalAddress = null,
        bool locationIsOnline = false)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var eventRecord = new EventRecord
        {
            Title = title ?? $"Tech Conference {suffix}",
            OrganizerId = organizerId,
            ScheduleStartsAt = startsAt ?? DateTimeOffset.UtcNow.AddDays(7),
            ScheduleEndsAt = (startsAt ?? DateTimeOffset.UtcNow.AddDays(7)).AddHours(2),
            ScheduleTimeZoneId = "UTC",
            LocationPhysicalAddress = locationIsOnline ? null : locationPhysicalAddress ?? "123 Conference Ave",
            LocationIsOnline = locationIsOnline,
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

    private async Task SeedTicketTypeAsync(
        int eventId,
        string name,
        decimal priceAmount,
        int capacity)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        databaseContext.TicketTypes.Add(new TicketTypeRecord
        {
            EventId = eventId,
            Name = name,
            PriceAmount = priceAmount,
            PriceCurrency = "VND",
            Capacity = capacity,
            Sold = 0,
            Reserved = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        });
        await databaseContext.SaveChangesAsync();
    }

    private async Task<string> PublishEventAsync(int eventId)
    {
        using var response = await _client.PostAsync($"/api/events/{eventId}/publish", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PublishEventResponse>(JsonOptions);
        return result!.Slug;
    }
}
