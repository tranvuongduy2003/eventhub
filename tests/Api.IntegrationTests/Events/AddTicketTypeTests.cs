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
public sealed class AddTicketTypeTests(IntegrationTestFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient(
        new WebApplicationFactoryClientOptions { HandleCookies = true });

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task AddTicketType_ValidRequest_Returns201WithTicketType()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId);

        var request = new AddTicketTypeRequest("General Admission", 50m, "VND", 100, null, null, null);

        using var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/ticket-types", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var ticketType = await response.Content.ReadFromJsonAsync<AddTicketTypeResponse>(JsonOptions);
        ticketType.Should().NotBeNull();
        ticketType!.Name.Should().Be("General Admission");
        ticketType.PriceAmount.Should().Be(50m);
        ticketType.PriceCurrency.Should().Be("VND");
        ticketType.Capacity.Should().Be(100);
        ticketType.Sold.Should().Be(0);
        ticketType.Reserved.Should().Be(0);
    }

    [Fact]
    public async Task AddTicketType_FreeTicket_Returns201()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId);

        var request = new AddTicketTypeRequest("Free Entry", 0m, "VND", 50, null, null, null);

        using var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/ticket-types", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var ticketType = await response.Content.ReadFromJsonAsync<AddTicketTypeResponse>(JsonOptions);
        ticketType.Should().NotBeNull();
        ticketType!.PriceAmount.Should().Be(0m);
    }

    [Fact]
    public async Task AddTicketType_NoAuth_Returns401()
    {
        var request = new AddTicketTypeRequest("VIP", 100m, "VND", 50, null, null, null);

        using var response = await _client.PostAsJsonAsync("/api/events/1/ticket-types", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddTicketType_NonOwner_Returns403()
    {
        var ownerUserId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(ownerUserId);

        // Register a second user (non-owner)
        await RegisterOrganizerAsync();

        var request = new AddTicketTypeRequest("VIP", 100m, "VND", 50, null, null, null);

        using var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/ticket-types", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AddTicketType_EventNotFound_Returns403()
    {
        await RegisterOrganizerAsync();

        var request = new AddTicketTypeRequest("VIP", 100m, "VND", 50, null, null, null);

        using var response = await _client.PostAsJsonAsync("/api/events/99999/ticket-types", request);

        // Auth check happens first — non-owner of event 99999 gets 403
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AddTicketType_EmptyName_Returns422()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId);

        var request = new AddTicketTypeRequest("", 50m, "VND", 100, null, null, null);

        using var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/ticket-types", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task AddTicketType_NegativePrice_Returns422()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId);

        var request = new AddTicketTypeRequest("VIP", -10m, "VND", 50, null, null, null);

        using var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/ticket-types", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task AddTicketType_ZeroCapacity_Returns422()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId);

        var request = new AddTicketTypeRequest("VIP", 100m, "VND", 0, null, null, null);

        using var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/ticket-types", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task AddTicketType_PublishedEvent_Returns422()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId);
        await SeedTicketTypeAsync(eventId);

        // Publish the event first
        using var publishResponse = await _client.PostAsync($"/api/events/{eventId}/publish", null);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var request = new AddTicketTypeRequest("Extra VIP", 200m, "VND", 10, null, null, null);

        using var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/ticket-types", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var responseBody = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<JsonElement>(responseBody, JsonOptions);
        problem.GetProperty("code").GetString().Should().Be("INVALID_EVENT_STATUS");
    }

    private async Task<Guid> RegisterOrganizerAsync(string? suffix = null)
    {
        suffix ??= Guid.NewGuid().ToString("N")[..8];
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

    private async Task SeedTicketTypeAsync(int eventId)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        databaseContext.TicketTypes.Add(new TicketTypeRecord
        {
            EventId = eventId,
            Name = "General Admission",
            PriceAmount = 50m,
            PriceCurrency = "VND",
            Capacity = 100,
            Sold = 0,
            Reserved = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        });
        await databaseContext.SaveChangesAsync();
    }
}
