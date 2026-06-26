using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EventHub.Api.IntegrationTests.Integration;
using EventHub.Contracts.Orders;
using EventHub.Contracts.Users;
using EventHub.Domain.Events;
using EventHub.Infrastructure.Persistence;
using EventHub.Infrastructure.Persistence.Entities;
using EventHub.Testing.Common.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.Api.IntegrationTests.Orders;

[Collection(IntegrationTestCollection.Name)]
public sealed class PlaceOrderMaxPerOrderTests(IntegrationTestFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient(
        new WebApplicationFactoryClientOptions { HandleCookies = true });

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task PlaceOrder_ExceedsMaxPerOrder_Returns422()
    {
        var eventId = await SeedPublishedEventWithTicketTypeAsync(maxPerOrder: 4);

        var request = new PlaceOrderRequest(
            "John Doe",
            "john@example.com",
            [new PlaceOrderLineRequest(1, 5)]);

        using var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/orders", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var responseBody = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<JsonElement>(responseBody, JsonOptions);
        problem.GetProperty("code").GetString().Should().Be("ORDER_MAX_PER_ORDER_EXCEEDED");
    }

    [Fact]
    public async Task PlaceOrder_WithinMaxPerOrder_Returns201()
    {
        var eventId = await SeedPublishedEventWithTicketTypeAsync(maxPerOrder: 4);

        var request = new PlaceOrderRequest(
            "John Doe",
            "john@example.com",
            [new PlaceOrderLineRequest(1, 4)]);

        using var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/orders", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task PlaceOrder_NoMaxPerOrder_Returns201()
    {
        var eventId = await SeedPublishedEventWithTicketTypeAsync(maxPerOrder: null);

        var request = new PlaceOrderRequest(
            "John Doe",
            "john@example.com",
            [new PlaceOrderLineRequest(1, 10)]);

        using var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/orders", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task PlaceOrder_MaxPerOrderOf1_ExceedsLimit_Returns422()
    {
        var eventId = await SeedPublishedEventWithTicketTypeAsync(maxPerOrder: 1);

        var request = new PlaceOrderRequest(
            "John Doe",
            "john@example.com",
            [new PlaceOrderLineRequest(1, 2)]);

        using var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/orders", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task PlaceOrder_MultipleTypes_IndependentLimits_Returns201()
    {
        var eventId = await SeedPublishedEventWithTwoTypesAsync(
            maxPerOrder1: 4, maxPerOrder2: 2);

        var request = new PlaceOrderRequest(
            "John Doe",
            "john@example.com",
            [
                new PlaceOrderLineRequest(1, 4),
                new PlaceOrderLineRequest(2, 2),
            ]);

        using var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/orders", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task PlaceOrder_MultipleTypes_OneExceedsLimit_Returns422()
    {
        var eventId = await SeedPublishedEventWithTwoTypesAsync(
            maxPerOrder1: 4, maxPerOrder2: 2);

        var request = new PlaceOrderRequest(
            "John Doe",
            "john@example.com",
            [
                new PlaceOrderLineRequest(1, 4),
                new PlaceOrderLineRequest(2, 3),
            ]);

        using var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/orders", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
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

    private async Task<int> SeedPublishedEventWithTicketTypeAsync(int? maxPerOrder)
    {
        var organizerId = await RegisterOrganizerAsync();

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
            Status = EventStatus.Published,
            Slug = $"tech-conf-{suffix}",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        databaseContext.Events.Add(eventRecord);
        await databaseContext.SaveChangesAsync();

        databaseContext.TicketTypes.Add(new TicketTypeRecord
        {
            EventId = eventRecord.Id,
            Name = "General Admission",
            PriceAmount = 0m,
            PriceCurrency = "VND",
            Capacity = 100,
            MaxPerOrder = maxPerOrder,
            Sold = 0,
            Reserved = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        });
        await databaseContext.SaveChangesAsync();

        return eventRecord.Id;
    }

    private async Task<int> SeedPublishedEventWithTwoTypesAsync(int? maxPerOrder1, int? maxPerOrder2)
    {
        var organizerId = await RegisterOrganizerAsync();

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
            Status = EventStatus.Published,
            Slug = $"tech-conf-{suffix}",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        databaseContext.Events.Add(eventRecord);
        await databaseContext.SaveChangesAsync();

        databaseContext.TicketTypes.Add(new TicketTypeRecord
        {
            EventId = eventRecord.Id,
            Name = "General",
            PriceAmount = 0m,
            PriceCurrency = "VND",
            Capacity = 100,
            MaxPerOrder = maxPerOrder1,
            Sold = 0,
            Reserved = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        });

        databaseContext.TicketTypes.Add(new TicketTypeRecord
        {
            EventId = eventRecord.Id,
            Name = "VIP",
            PriceAmount = 0m,
            PriceCurrency = "VND",
            Capacity = 50,
            MaxPerOrder = maxPerOrder2,
            Sold = 0,
            Reserved = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        });
        await databaseContext.SaveChangesAsync();

        return eventRecord.Id;
    }
}
