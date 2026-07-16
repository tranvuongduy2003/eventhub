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
using FluentAssertions.Execution;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.Api.IntegrationTests.Orders;

[Collection(IntegrationTestCollection.Name)]
public sealed class PlaceOrderMaxPerOrderTests(IntegrationTestFixture fixture)
{
    private sealed record EventData(int EventId, int TicketTypeId);
    private sealed record EventDataWithTwoTypes(int EventId, int Type1Id, int Type2Id);

    private readonly HttpClient _client = fixture.Factory.CreateClient(
        new WebApplicationFactoryClientOptions { HandleCookies = true });

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task PlaceOrder_ExceedsMaxPerOrder_Returns422()
    {
        var data = await SeedPublishedEventWithTicketTypeAsync(maxPerOrder: 4);

        var request = new PlaceOrderRequest(
            "John Doe",
            "john@example.com",
            [new PlaceOrderLineRequest(data.TicketTypeId, 5)]);

        using var response = await _client.PostAsJsonAsync($"/api/events/{data.EventId}/orders", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var responseBody = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<JsonElement>(responseBody, JsonOptions);
        problem.GetProperty("code").GetString().Should().Be("ORDER_MAX_PER_ORDER_EXCEEDED");
    }

    [Fact]
    public async Task PlaceOrder_DuplicateLinesWithCombinedQuantityExceedingMaxPerOrder_Returns422WithoutPersistingOrder()
    {
        var data = await SeedPublishedEventWithTicketTypeAsync(maxPerOrder: 1);

        var request = new PlaceOrderRequest(
            "John Doe",
            "john@example.com",
            [
                new PlaceOrderLineRequest(data.TicketTypeId, 1),
                new PlaceOrderLineRequest(data.TicketTypeId, 1),
            ]);

        using var response = await _client.PostAsJsonAsync($"/api/events/{data.EventId}/orders", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var responseBody = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<JsonElement>(responseBody, JsonOptions);
        problem.GetProperty("code").GetString().Should().Be("ORDER_MAX_PER_ORDER_EXCEEDED");

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        (await databaseContext.Orders.CountAsync(order => order.EventId == data.EventId)).Should().Be(0);
    }

    [Fact]
    public async Task PlaceOrder_DuplicateLinesWithinCombinedLimit_PersistsOneOrderLineAndReservation()
    {
        var data = await SeedPublishedEventWithTicketTypeAsync(maxPerOrder: 4, priceAmount: 50m);

        var request = new PlaceOrderRequest(
            "John Doe",
            "john@example.com",
            [
                new PlaceOrderLineRequest(data.TicketTypeId, 2),
                new PlaceOrderLineRequest(data.TicketTypeId, 2),
            ]);

        using var response = await _client.PostAsJsonAsync($"/api/events/{data.EventId}/orders", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await response.Content.ReadFromJsonAsync<PlaceOrderResponse>();
        order.Should().NotBeNull();
        order!.Lines.Should().ContainSingle();
        order.Lines[0].TicketTypeId.Should().Be(data.TicketTypeId);
        order.Lines[0].Quantity.Should().Be(4);
        order.TotalAmount.Should().Be(200m);

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var orderLines = await databaseContext.OrderLines
            .Where(line => line.OrderId == order.OrderId)
            .ToListAsync();
        var reservations = await databaseContext.Reservations
            .Where(reservation => reservation.OrderId == order.OrderId)
            .ToListAsync();

        orderLines.Should().ContainSingle();
        orderLines[0].TicketTypeId.Should().Be(data.TicketTypeId);
        orderLines[0].Quantity.Should().Be(4);
        reservations.Should().ContainSingle();
        reservations[0].TicketTypeId.Should().Be(data.TicketTypeId);
        reservations[0].Quantity.Should().Be(4);
    }

    [Fact]
    public async Task PlaceOrder_WithinMaxPerOrder_Returns201()
    {
        var data = await SeedPublishedEventWithTicketTypeAsync(maxPerOrder: 4);

        var request = new PlaceOrderRequest(
            "John Doe",
            "john@example.com",
            [new PlaceOrderLineRequest(data.TicketTypeId, 4)]);

        using var response = await _client.PostAsJsonAsync($"/api/events/{data.EventId}/orders", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task PlaceOrder_NoMaxPerOrder_Returns201()
    {
        var data = await SeedPublishedEventWithTicketTypeAsync(maxPerOrder: null);

        var request = new PlaceOrderRequest(
            "John Doe",
            "john@example.com",
            [new PlaceOrderLineRequest(data.TicketTypeId, 10)]);

        using var response = await _client.PostAsJsonAsync($"/api/events/{data.EventId}/orders", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task PlaceOrder_MaxPerOrderOf1_ExceedsLimit_Returns422()
    {
        var data = await SeedPublishedEventWithTicketTypeAsync(maxPerOrder: 1);

        var request = new PlaceOrderRequest(
            "John Doe",
            "john@example.com",
            [new PlaceOrderLineRequest(data.TicketTypeId, 2)]);

        using var response = await _client.PostAsJsonAsync($"/api/events/{data.EventId}/orders", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task PlaceOrder_MultipleTypes_IndependentLimits_Returns201()
    {
        var data = await SeedPublishedEventWithTwoTypesAsync(
            maxPerOrder1: 4, maxPerOrder2: 2);

        var request = new PlaceOrderRequest(
            "John Doe",
            "john@example.com",
            [
                new PlaceOrderLineRequest(data.Type1Id, 4),
                new PlaceOrderLineRequest(data.Type2Id, 2),
            ]);

        using var response = await _client.PostAsJsonAsync($"/api/events/{data.EventId}/orders", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task PlaceOrder_FreeOrderWithMultipleTicketTypes_ConfirmsEveryReservation()
    {
        var data = await SeedPublishedEventWithTwoTypesAsync(
            maxPerOrder1: 4,
            maxPerOrder2: 3);

        var request = new PlaceOrderRequest(
            "John Doe",
            "john@example.com",
            [
                new PlaceOrderLineRequest(data.Type1Id, 2),
                new PlaceOrderLineRequest(data.Type2Id, 3),
            ]);

        using var response = await _client.PostAsJsonAsync($"/api/events/{data.EventId}/orders", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await response.Content.ReadFromJsonAsync<PlaceOrderResponse>();
        order.Should().NotBeNull();
        order!.Status.Should().Be("confirmed");

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var firstTicketType = await databaseContext.TicketTypes.SingleAsync(type => type.Id == data.Type1Id);
        var secondTicketType = await databaseContext.TicketTypes.SingleAsync(type => type.Id == data.Type2Id);

        firstTicketType.Reserved.Should().Be(0);
        firstTicketType.Sold.Should().Be(2);
        secondTicketType.Reserved.Should().Be(0);
        secondTicketType.Sold.Should().Be(3);
        (await databaseContext.Reservations.CountAsync(reservation => reservation.OrderId == order.OrderId)).Should().Be(0);
    }

    [Fact]
    public async Task PlaceOrder_ConcurrentFinalTicketRequests_CreateExactlyOneOrder()
    {
        var data = await SeedPublishedEventWithTicketTypeAsync(maxPerOrder: 1, capacity: 1);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var purchaseTasks = Enumerable.Range(0, 8)
            .Select(async attempt =>
            {
                using var client = fixture.Factory.CreateClient();
                await start.Task;

                using var response = await client.PostAsJsonAsync(
                    $"/api/events/{data.EventId}/orders",
                    new PlaceOrderRequest(
                        $"Attendee {attempt}",
                        $"attendee{attempt}@example.com",
                        [new PlaceOrderLineRequest(data.TicketTypeId, 1)]));

                return response.StatusCode;
            })
            .ToList();

        start.SetResult();
        var statuses = await Task.WhenAll(purchaseTasks);

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var ticketType = await databaseContext.TicketTypes.SingleAsync(type => type.Id == data.TicketTypeId);

        using var assertions = new AssertionScope();
        statuses.Count(status => status == HttpStatusCode.Created).Should().Be(1);
        statuses.Should().OnlyContain(status =>
            status == HttpStatusCode.Created
            || status == HttpStatusCode.UnprocessableEntity
            || status == HttpStatusCode.Conflict);
        ticketType.Sold.Should().Be(1);
        ticketType.Reserved.Should().Be(0);
        (await databaseContext.Orders.CountAsync(order => order.EventId == data.EventId)).Should().Be(1);
        (await databaseContext.Reservations.CountAsync(reservation => reservation.EventId == data.EventId)).Should().Be(0);
    }

    [Fact]
    public async Task PlaceOrder_MultipleTypes_OneExceedsLimit_Returns422()
    {
        var data = await SeedPublishedEventWithTwoTypesAsync(
            maxPerOrder1: 4, maxPerOrder2: 2);

        var request = new PlaceOrderRequest(
            "John Doe",
            "john@example.com",
            [
                new PlaceOrderLineRequest(data.Type1Id, 4),
                new PlaceOrderLineRequest(data.Type2Id, 3),
            ]);

        using var response = await _client.PostAsJsonAsync($"/api/events/{data.EventId}/orders", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    private async Task<Guid> RegisterOrganizerAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
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

    private async Task<EventData> SeedPublishedEventWithTicketTypeAsync(
        int? maxPerOrder,
        decimal priceAmount = 0m,
        int capacity = 100)
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

        var ticketTypeRecord = new TicketTypeRecord
        {
            EventId = eventRecord.Id,
            Name = "General Admission",
            PriceAmount = priceAmount,
            PriceCurrency = "VND",
            Capacity = capacity,
            MaxPerOrder = maxPerOrder,
            Sold = 0,
            Reserved = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        databaseContext.TicketTypes.Add(ticketTypeRecord);
        await databaseContext.SaveChangesAsync();

        return new EventData(eventRecord.Id, ticketTypeRecord.Id);
    }

    private async Task<EventDataWithTwoTypes> SeedPublishedEventWithTwoTypesAsync(int? maxPerOrder1, int? maxPerOrder2)
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

        var type1 = new TicketTypeRecord
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
        };

        var type2 = new TicketTypeRecord
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
        };

        databaseContext.TicketTypes.Add(type1);
        databaseContext.TicketTypes.Add(type2);
        await databaseContext.SaveChangesAsync();

        return new EventDataWithTwoTypes(eventRecord.Id, type1.Id, type2.Id);
    }
}
