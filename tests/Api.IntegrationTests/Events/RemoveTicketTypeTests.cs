using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EventHub.Api.IntegrationTests.Integration;
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
public sealed class RemoveTicketTypeTests(IntegrationTestFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient(
        new WebApplicationFactoryClientOptions { HandleCookies = true });

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task RemoveTicketType_NoSales_Returns204()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId);
        var ticketTypeId = await SeedTicketTypeAsync(eventId);

        using var response = await _client.DeleteAsync(
            $"/api/events/{eventId}/ticket-types/{ticketTypeId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RemoveTicketType_NoAuth_Returns401()
    {
        using var response = await _client.DeleteAsync(
            "/api/events/1/ticket-types/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RemoveTicketType_NonOwner_Returns403()
    {
        var ownerUserId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(ownerUserId);
        var ticketTypeId = await SeedTicketTypeAsync(eventId);

        await RegisterOrganizerAsync();

        using var response = await _client.DeleteAsync(
            $"/api/events/{eventId}/ticket-types/{ticketTypeId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RemoveTicketType_HasSoldTickets_Returns422()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId);
        var ticketTypeId = await SeedTicketTypeAsync(eventId, sold: 5);

        using var response = await _client.DeleteAsync(
            $"/api/events/{eventId}/ticket-types/{ticketTypeId}");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var responseBody = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<JsonElement>(responseBody, JsonOptions);
        problem.GetProperty("code").GetString().Should().Be("TICKET_TYPE_HAS_SALES");
    }

    [Fact]
    public async Task RemoveTicketType_HasReservedTickets_Returns422()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId);
        var ticketTypeId = await SeedTicketTypeAsync(eventId, reserved: 3);

        using var response = await _client.DeleteAsync(
            $"/api/events/{eventId}/ticket-types/{ticketTypeId}");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var responseBody = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<JsonElement>(responseBody, JsonOptions);
        problem.GetProperty("code").GetString().Should().Be("TICKET_TYPE_HAS_SALES");
    }

    [Fact]
    public async Task RemoveTicketType_LastOnPublishedEvent_Returns422()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId);
        var ticketTypeId = await SeedTicketTypeAsync(eventId);

        using var publishResponse = await _client.PostAsync($"/api/events/{eventId}/publish", null);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var response = await _client.DeleteAsync(
            $"/api/events/{eventId}/ticket-types/{ticketTypeId}");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var responseBody = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<JsonElement>(responseBody, JsonOptions);
        problem.GetProperty("code").GetString().Should().Be("TICKET_TYPE_LAST_ON_PUBLISHED_EVENT");
    }

    [Fact]
    public async Task RemoveTicketType_NotFound_Returns422()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId);

        using var response = await _client.DeleteAsync(
            $"/api/events/{eventId}/ticket-types/99999");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var responseBody = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<JsonElement>(responseBody, JsonOptions);
        problem.GetProperty("code").GetString().Should().Be("TICKET_TYPE_NOT_FOUND");
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

    private async Task<int> SeedTicketTypeAsync(
        int eventId,
        string name = "General Admission",
        decimal price = 50m,
        int capacity = 100,
        int sold = 0,
        int reserved = 0)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        var record = new TicketTypeRecord
        {
            EventId = eventId,
            Name = name,
            PriceAmount = price,
            PriceCurrency = "VND",
            Capacity = capacity,
            Sold = sold,
            Reserved = reserved,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        databaseContext.TicketTypes.Add(record);
        await databaseContext.SaveChangesAsync();

        return record.Id;
    }
}
