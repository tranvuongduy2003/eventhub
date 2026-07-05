using System.Net;
using System.Net.Http.Json;
using EventHub.Api.IntegrationTests.Integration;
using EventHub.Contracts.Orders;
using EventHub.Contracts.Users;
using EventHub.Domain.Events;
using EventHub.Infrastructure.Persistence;
using EventHub.Infrastructure.Persistence.Entities;
using EventHub.Testing.Common.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.Api.IntegrationTests.Orders;

[Collection(IntegrationTestCollection.Name)]
public sealed class GuestCheckoutTests(IntegrationTestFixture fixture)
{
    private sealed record EventData(string Slug, int EventId, int TicketTypeId);

    private readonly HttpClient _organizerClient = fixture.Factory.CreateClient(
        new WebApplicationFactoryClientOptions { HandleCookies = true });

    private readonly HttpClient _guestClient = fixture.Factory.CreateClient();

    [Fact]
    public async Task PlaceOrder_AnonymousBuyerWithGuestContact_Returns201()
    {
        var data = await SeedPublishedEventAsync();
        var request = new PlaceOrderRequest(
            "Jane Attendee",
            "jane@example.com",
            [new PlaceOrderLineRequest(data.TicketTypeId, 2)]);

        using var response = await _guestClient.PostAsJsonAsync(
            $"/api/events/{data.EventId}/orders",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var order = await databaseContext.Orders.SingleAsync(order => order.EventId == data.EventId);
        order.ContactName.Should().Be("Jane Attendee");
        order.ContactEmail.Should().Be("jane@example.com");
    }

    [Fact]
    public async Task PlaceOrder_PaidOrder_CreatesPendingOrderAndInventoryHold()
    {
        var data = await SeedPublishedEventAsync(capacity: 3, priceAmount: 50m);
        var request = new PlaceOrderRequest(
            "Jane Attendee",
            "jane@example.com",
            [new PlaceOrderLineRequest(data.TicketTypeId, 2)]);

        using var response = await _guestClient.PostAsJsonAsync(
            $"/api/events/{data.EventId}/orders",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var order = await databaseContext.Orders.SingleAsync(order => order.EventId == data.EventId);
        var reservation = await databaseContext.Reservations.SingleAsync(reservation => reservation.EventId == data.EventId);
        var ticketType = await databaseContext.TicketTypes.SingleAsync(ticketType => ticketType.Id == data.TicketTypeId);

        order.Status.Should().Be("Pending");
        order.ExpiresAt.Should().NotBeNull();
        order.ExpiresAt.Should().Be(reservation.ExpiresAt);
        reservation.Quantity.Should().Be(2);
        reservation.OrderId.Should().Be(order.Id);
        ticketType.Reserved.Should().Be(2);

        var nextBuyerRequest = new StartCheckoutRequest([new StartCheckoutLineRequest(data.TicketTypeId, 2)]);
        using var nextBuyerResponse = await _guestClient.PostAsJsonAsync(
            $"/api/events/{data.Slug}/checkout/start",
            nextBuyerRequest);

        nextBuyerResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetOrderStatus_AfterTicketPriceChanges_ReturnsFinalSummaryFromOrderSnapshot()
    {
        var data = await SeedPublishedEventAsync(capacity: 5, priceAmount: 50m);
        var request = new PlaceOrderRequest(
            "Jane Attendee",
            "jane@example.com",
            [new PlaceOrderLineRequest(data.TicketTypeId, 2)]);

        using var placeResponse = await _guestClient.PostAsJsonAsync(
            $"/api/events/{data.EventId}/orders",
            request);

        placeResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var placedOrder = await placeResponse.Content.ReadFromJsonAsync<PlaceOrderResponse>();
        placedOrder.Should().NotBeNull();
        placedOrder!.TotalAmount.Should().Be(100m);
        placedOrder.Lines.Should().ContainSingle(line =>
            line.TicketTypeName == "General Admission"
            && line.Quantity == 2
            && line.UnitPriceAmount == 50m
            && line.LineTotalAmount == 100m);

        await using (var scope = fixture.Factory.Services.CreateAsyncScope())
        {
            var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
            var ticketType = await databaseContext.TicketTypes.SingleAsync(ticketType => ticketType.Id == data.TicketTypeId);
            ticketType.PriceAmount = 75m;
            await databaseContext.SaveChangesAsync();
        }

        using var statusResponse = await _guestClient.GetAsync($"/api/orders/{placedOrder.OrderId}");

        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var status = await statusResponse.Content.ReadFromJsonAsync<OrderStatusResponse>();
        status.Should().NotBeNull();
        status!.TotalAmount.Should().Be(100m);
        status.Lines.Should().ContainSingle(line =>
            line.TicketTypeName == "General Admission"
            && line.Quantity == 2
            && line.UnitPriceAmount == 50m
            && line.LineTotalAmount == 100m);
    }

    [Fact]
    public async Task PlaceOrder_MissingGuestName_Returns422()
    {
        var data = await SeedPublishedEventAsync();
        var request = new PlaceOrderRequest(
            "",
            "jane@example.com",
            [new PlaceOrderLineRequest(data.TicketTypeId, 1)]);

        using var response = await _guestClient.PostAsJsonAsync(
            $"/api/events/{data.EventId}/orders",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        (await databaseContext.Orders.CountAsync(order => order.EventId == data.EventId)).Should().Be(0);
    }

    [Fact]
    public async Task PlaceOrder_InvalidGuestEmail_Returns422()
    {
        var data = await SeedPublishedEventAsync();
        var request = new PlaceOrderRequest(
            "Jane Attendee",
            "not-an-email",
            [new PlaceOrderLineRequest(data.TicketTypeId, 1)]);

        using var response = await _guestClient.PostAsJsonAsync(
            $"/api/events/{data.EventId}/orders",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        (await databaseContext.Orders.CountAsync(order => order.EventId == data.EventId)).Should().Be(0);
    }

    private async Task<Guid> RegisterOrganizerAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"organizer_{suffix}@example.com";
        var request = new RegisterUserRequest(
            $"Organizer_{suffix}",
            email,
            "SecurePass1!");

        using var response = await _organizerClient.PostAsJsonAsync("/api/users", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var user = databaseContext.Users.Single(u => u.Email == email);
        return user.Id;
    }

    private async Task<EventData> SeedPublishedEventAsync(int capacity = 10, decimal priceAmount = 0m)
    {
        var organizerId = await RegisterOrganizerAsync();

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var eventRecord = new EventRecord
        {
            Title = $"Guest Checkout Event {suffix}",
            OrganizerId = organizerId,
            ScheduleStartsAt = new DateTimeOffset(2026, 7, 15, 14, 0, 0, TimeSpan.Zero),
            ScheduleEndsAt = new DateTimeOffset(2026, 7, 15, 16, 0, 0, TimeSpan.Zero),
            ScheduleTimeZoneId = "UTC",
            LocationPhysicalAddress = "123 Checkout Ave",
            LocationIsOnline = false,
            Status = EventStatus.Published,
            Slug = $"guest-checkout-{suffix}",
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
            MaxPerOrder = 4,
            Sold = 0,
            Reserved = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        databaseContext.TicketTypes.Add(ticketTypeRecord);
        await databaseContext.SaveChangesAsync();

        return new EventData(eventRecord.Slug, eventRecord.Id, ticketTypeRecord.Id);
    }
}
