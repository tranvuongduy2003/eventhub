using System.Net;
using System.Net.Http.Json;
using EventHub.Api.IntegrationTests.Integration;
using EventHub.Application.Abstractions.Services;
using EventHub.Contracts.Orders;
using EventHub.Contracts.Users;
using EventHub.Domain.Events;
using EventHub.Infrastructure.Persistence;
using EventHub.Infrastructure.Persistence.Entities;
using EventHub.Testing.Common.Fixtures;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace EventHub.Api.IntegrationTests.Orders;

[Collection(IntegrationTestCollection.Name)]
public sealed class HoldExpiryTests(IntegrationTestFixture fixture)
{
    private static readonly DateTimeOffset Start = new(2026, 7, 5, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ProcessExpiredOrderHolds_DuePendingOrder_ExpiresOrderAndReleasesReservation()
    {
        var clock = new TestClock { UtcNow = Start };
        await using var factory = CreateFactory(clock);
        await ClearCheckoutDataAsync(factory);
        var guestClient = factory.CreateClient();
        var organizerClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        var data = await SeedPublishedEventAsync(factory, organizerClient, capacity: 3, priceAmount: 50m);
        var orderId = await PlacePaidOrderAsync(guestClient, data.EventId, data.TicketTypeId, quantity: 2);

        clock.UtcNow = Start.AddMinutes(16);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            var result = await sender.Send(new EventHub.Application.Orders.Commands.ProcessExpiredOrderHoldsCommand());

            result.IsSuccess.Should().BeTrue();
            result.Value!.ExpiredOrderCount.Should().Be(1);
            result.Value.ReleasedReservationCount.Should().Be(1);
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
            var order = await databaseContext.Orders.SingleAsync(order => order.Id == orderId);
            var ticketType = await databaseContext.TicketTypes.SingleAsync(ticketType => ticketType.Id == data.TicketTypeId);

            order.Status.Should().Be("Expired");
            order.ReservationId.Should().BeNull();
            ticketType.Reserved.Should().Be(0);
            (await databaseContext.Reservations.CountAsync(reservation => reservation.OrderId == orderId)).Should().Be(0);
        }
    }

    [Fact]
    public async Task ProcessExpiredOrderHolds_FuturePendingOrder_DoesNotChangeOrderOrInventory()
    {
        var clock = new TestClock { UtcNow = Start };
        await using var factory = CreateFactory(clock);
        await ClearCheckoutDataAsync(factory);
        var guestClient = factory.CreateClient();
        var organizerClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        var data = await SeedPublishedEventAsync(factory, organizerClient, capacity: 3, priceAmount: 50m);
        var orderId = await PlacePaidOrderAsync(guestClient, data.EventId, data.TicketTypeId, quantity: 2);

        clock.UtcNow = Start.AddMinutes(10);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            var result = await sender.Send(new EventHub.Application.Orders.Commands.ProcessExpiredOrderHoldsCommand());

            result.IsSuccess.Should().BeTrue();
            result.Value!.ExpiredOrderCount.Should().Be(0);
            result.Value.ReleasedReservationCount.Should().Be(0);
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
            var order = await databaseContext.Orders.SingleAsync(order => order.Id == orderId);
            var ticketType = await databaseContext.TicketTypes.SingleAsync(ticketType => ticketType.Id == data.TicketTypeId);

            order.Status.Should().Be("Pending");
            order.ReservationId.Should().NotBeNull();
            ticketType.Reserved.Should().Be(2);
            (await databaseContext.Reservations.CountAsync(reservation => reservation.OrderId == orderId)).Should().Be(1);
        }
    }

    private IntegrationTestWebApplicationFactory CreateFactory(TestClock clock) =>
        fixture.CreateFactory(services =>
        {
            services.RemoveAll<IClock>();
            services.AddSingleton<IClock>(clock);
            services.RemoveAll<IHostedService>();
        });

    private static async Task ClearCheckoutDataAsync(IntegrationTestWebApplicationFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        await databaseContext.Reservations.ExecuteDeleteAsync();
        await databaseContext.OrderLines.ExecuteDeleteAsync();
        await databaseContext.Orders.ExecuteDeleteAsync();
    }

    private static async Task<int> PlacePaidOrderAsync(
        HttpClient guestClient,
        int eventId,
        int ticketTypeId,
        int quantity)
    {
        var request = new PlaceOrderRequest(
            "Jane Attendee",
            "jane@example.com",
            [new PlaceOrderLineRequest(ticketTypeId, quantity)]);

        using var response = await guestClient.PostAsJsonAsync($"/api/events/{eventId}/orders", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<PlaceOrderResponse>();
        body.Should().NotBeNull();
        return body!.OrderId;
    }

    private static async Task<EventData> SeedPublishedEventAsync(
        IntegrationTestWebApplicationFactory factory,
        HttpClient organizerClient,
        int capacity,
        decimal priceAmount)
    {
        var organizerId = await RegisterOrganizerAsync(factory, organizerClient);

        await using var scope = factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var eventRecord = new EventRecord
        {
            Title = $"Hold Expiry Event {suffix}",
            OrganizerId = organizerId,
            ScheduleStartsAt = new DateTimeOffset(2026, 7, 15, 14, 0, 0, TimeSpan.Zero),
            ScheduleEndsAt = new DateTimeOffset(2026, 7, 15, 16, 0, 0, TimeSpan.Zero),
            ScheduleTimeZoneId = "UTC",
            LocationPhysicalAddress = "123 Expiry Ave",
            LocationIsOnline = false,
            Status = EventStatus.Published,
            Slug = $"hold-expiry-{suffix}",
            CreatedAt = Start,
            UpdatedAt = Start,
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
            CreatedAt = Start,
            UpdatedAt = Start,
        };

        databaseContext.TicketTypes.Add(ticketTypeRecord);
        await databaseContext.SaveChangesAsync();

        return new EventData(eventRecord.Id, ticketTypeRecord.Id);
    }

    private static async Task<Guid> RegisterOrganizerAsync(
        IntegrationTestWebApplicationFactory factory,
        HttpClient organizerClient)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"hold_expiry_{suffix}@example.com";
        var request = new RegisterUserRequest(
            $"Organizer_{suffix}",
            email,
            "SecurePass1!");

        using var response = await organizerClient.PostAsJsonAsync("/api/users", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        await using var scope = factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var user = await databaseContext.Users.SingleAsync(user => user.Email == email);
        return user.Id;
    }

    private sealed record EventData(int EventId, int TicketTypeId);
}
