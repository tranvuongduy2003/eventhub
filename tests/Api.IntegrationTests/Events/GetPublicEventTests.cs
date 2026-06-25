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
public sealed class GetPublicEventTests(IntegrationTestFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient(
        new WebApplicationFactoryClientOptions { HandleCookies = true });

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task GetPublicEvent_PublishedEvent_Returns200WithTicketTypes()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId);
        await SeedTicketTypeAsync(eventId, "General Admission", 50m, 100);
        await SeedTicketTypeAsync(eventId, "VIP", 150m, 50);
        await PublishEventAsync(eventId);

        using var response = await _client.GetAsync($"/api/events/{eventId}/public");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PublicEventResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.EventId.Should().Be(eventId);
        result.Title.Should().StartWith("Tech Conference");
        result.TicketTypes.Should().HaveCount(2);
        result.TicketTypes.Should().Contain(tt => tt.Name == "General Admission" && tt.PriceAmount == 50m);
        result.TicketTypes.Should().Contain(tt => tt.Name == "VIP" && tt.PriceAmount == 150m);
    }

    [Fact]
    public async Task GetPublicEvent_PublishedEvent_ReturnsTicketAvailability()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId);
        await SeedTicketTypeAsync(eventId, "General Admission", 50m, 100, sold: 30, reserved: 5);
        await PublishEventAsync(eventId);

        using var response = await _client.GetAsync($"/api/events/{eventId}/public");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PublicEventResponse>(JsonOptions);
        result.Should().NotBeNull();

        var ticketType = result!.TicketTypes.Single();
        ticketType.Capacity.Should().Be(100);
        ticketType.Sold.Should().Be(30);
        ticketType.Reserved.Should().Be(5);
        ticketType.IsSoldOut.Should().BeFalse();
    }

    [Fact]
    public async Task GetPublicEvent_SoldOutTicketType_ReturnsIsSoldOutTrue()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId);
        await SeedTicketTypeAsync(eventId, "General Admission", 50m, 100, sold: 100, reserved: 0);
        await PublishEventAsync(eventId);

        using var response = await _client.GetAsync($"/api/events/{eventId}/public");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PublicEventResponse>(JsonOptions);
        result.Should().NotBeNull();

        var ticketType = result!.TicketTypes.Single();
        ticketType.IsSoldOut.Should().BeTrue();
    }

    [Fact]
    public async Task GetPublicEvent_FreeEvent_ReturnsPriceAmountZero()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId);
        await SeedTicketTypeAsync(eventId, "Free Entry", 0m, 200);
        await PublishEventAsync(eventId);

        using var response = await _client.GetAsync($"/api/events/{eventId}/public");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PublicEventResponse>(JsonOptions);
        result.Should().NotBeNull();

        var ticketType = result!.TicketTypes.Single();
        ticketType.PriceAmount.Should().Be(0m);
        ticketType.PriceCurrency.Should().Be("VND");
    }

    [Fact]
    public async Task GetPublicEvent_DraftEvent_Returns404()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId);
        await SeedTicketTypeAsync(eventId, "General Admission", 50m, 100);

        using var response = await _client.GetAsync($"/api/events/{eventId}/public");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPublicEvent_CancelledEvent_Returns404()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId);
        await SeedTicketTypeAsync(eventId, "General Admission", 50m, 100);
        await PublishEventAsync(eventId);

        // Cancel via API
        using var cancelResponse = await _client.PostAsync($"/api/events/{eventId}/cancel", null);
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var response = await _client.GetAsync($"/api/events/{eventId}/public");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPublicEvent_NoAuth_Returns200()
    {
        // Create an unauthenticated client
        using var unauthenticatedClient = fixture.Factory.CreateClient();

        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId);
        await SeedTicketTypeAsync(eventId, "General Admission", 50m, 100);
        await PublishEventAsync(eventId);

        using var response = await unauthenticatedClient.GetAsync($"/api/events/{eventId}/public");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PublicEventResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.EventId.Should().Be(eventId);
    }

    [Fact]
    public async Task GetPublicEvent_NonExistentEvent_Returns404()
    {
        using var response = await _client.GetAsync("/api/events/99999/public");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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

    private async Task SeedTicketTypeAsync(
        int eventId,
        string name,
        decimal priceAmount,
        int capacity,
        int sold = 0,
        int reserved = 0)
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
            Sold = sold,
            Reserved = reserved,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        });
        await databaseContext.SaveChangesAsync();
    }

    private async Task PublishEventAsync(int eventId)
    {
        using var response = await _client.PostAsync($"/api/events/{eventId}/publish", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
