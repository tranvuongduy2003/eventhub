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

namespace EventHub.Api.IntegrationTests.TicketTypes;

[Collection(IntegrationTestCollection.Name)]
public sealed class MaxPerOrderTests(IntegrationTestFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient(
        new WebApplicationFactoryClientOptions { HandleCookies = true });

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task AddTicketType_WithMaxPerOrder_Returns201WithLimit()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId);

        var request = new AddTicketTypeRequest("VIP", 100m, "VND", 50, 4, null, null);

        using var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/ticket-types", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var ticketType = await response.Content.ReadFromJsonAsync<AddTicketTypeResponse>(JsonOptions);
        ticketType.Should().NotBeNull();
        ticketType!.MaxPerOrder.Should().Be(4);
    }

    [Fact]
    public async Task AddTicketType_WithNullMaxPerOrder_Returns201WithNull()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId);

        var request = new AddTicketTypeRequest("General", 50m, "VND", 100, null, null, null);

        using var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/ticket-types", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var ticketType = await response.Content.ReadFromJsonAsync<AddTicketTypeResponse>(JsonOptions);
        ticketType.Should().NotBeNull();
        ticketType!.MaxPerOrder.Should().BeNull();
    }

    [Fact]
    public async Task AddTicketType_WithZeroMaxPerOrder_Returns422()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId);

        var request = new AddTicketTypeRequest("VIP", 100m, "VND", 50, 0, null, null);

        using var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/ticket-types", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task AddTicketType_WithNegativeMaxPerOrder_Returns422()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId);

        var request = new AddTicketTypeRequest("VIP", 100m, "VND", 50, -1, null, null);

        using var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/ticket-types", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task EditTicketType_SetMaxPerOrder_Returns200WithLimit()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId);
        var ticketTypeId = await SeedTicketTypeAsync(eventId);

        var request = new EditTicketTypeRequest("General Admission", 50m, "VND", 100, 4, null, null);

        using var response = await _client.PutAsJsonAsync(
            $"/api/events/{eventId}/ticket-types/{ticketTypeId}", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var ticketType = await response.Content.ReadFromJsonAsync<EditTicketTypeResponse>(JsonOptions);
        ticketType.Should().NotBeNull();
        ticketType!.MaxPerOrder.Should().Be(4);
    }

    [Fact]
    public async Task EditTicketType_ClearMaxPerOrder_Returns200WithNull()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId);
        var ticketTypeId = await SeedTicketTypeAsync(eventId, maxPerOrder: 4);

        var request = new EditTicketTypeRequest("General Admission", 50m, "VND", 100, null, null, null);

        using var response = await _client.PutAsJsonAsync(
            $"/api/events/{eventId}/ticket-types/{ticketTypeId}", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var ticketType = await response.Content.ReadFromJsonAsync<EditTicketTypeResponse>(JsonOptions);
        ticketType.Should().NotBeNull();
        ticketType!.MaxPerOrder.Should().BeNull();
    }

    [Fact]
    public async Task EditTicketType_PublishedEvent_MaxPerOrderOnly_Returns200()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId);
        var ticketTypeId = await SeedTicketTypeAsync(eventId);

        // Publish the event
        using var publishResponse = await _client.PostAsync($"/api/events/{eventId}/publish", null);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Only change MaxPerOrder — same name, price, capacity
        var request = new EditTicketTypeRequest("General Admission", 50m, "VND", 100, 4, null, null);

        using var response = await _client.PutAsJsonAsync(
            $"/api/events/{eventId}/ticket-types/{ticketTypeId}", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var ticketType = await response.Content.ReadFromJsonAsync<EditTicketTypeResponse>(JsonOptions);
        ticketType.Should().NotBeNull();
        ticketType!.MaxPerOrder.Should().Be(4);
    }

    [Fact]
    public async Task EditTicketType_PublishedEvent_NameChange_Returns422()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId);
        var ticketTypeId = await SeedTicketTypeAsync(eventId);

        using var publishResponse = await _client.PostAsync($"/api/events/{eventId}/publish", null);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Change name AND MaxPerOrder — should fail because name changed
        var request = new EditTicketTypeRequest("Updated Name", 50m, "VND", 100, 4, null, null);

        using var response = await _client.PutAsJsonAsync(
            $"/api/events/{eventId}/ticket-types/{ticketTypeId}", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    private async Task<Guid> RegisterOrganizerAsync(string? suffix = null)
    {
        suffix ??= Guid.NewGuid().ToString("N")[..8];
        var email = $"organizer_{suffix}@example.com";
        var request = new RegisterUserRequest(
            $"Organizer_{suffix}",
            email,
            "SecurePass1!");

        using var response = await _client.PostAsJsonAsync("/api/users", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var user = databaseContext.Users.Single(u => u.Email == email);
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

    private async Task<int> SeedTicketTypeAsync(int eventId, int? maxPerOrder = null)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        var record = new TicketTypeRecord
        {
            EventId = eventId,
            Name = "General Admission",
            PriceAmount = 50m,
            PriceCurrency = "VND",
            Capacity = 100,
            MaxPerOrder = maxPerOrder,
            Sold = 0,
            Reserved = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        databaseContext.TicketTypes.Add(record);
        await databaseContext.SaveChangesAsync();

        return record.Id;
    }
}
