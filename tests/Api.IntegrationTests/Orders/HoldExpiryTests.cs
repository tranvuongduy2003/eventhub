using System.Net;
using System.Net.Http.Json;
using EventHub.Api.IntegrationTests.Integration;
using EventHub.Application.Abstractions.Services;
using EventHub.Contracts.Orders;
using EventHub.Contracts.Users;
using EventHub.Domain.Events;
using EventHub.Domain.Orders;
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
    public async Task ProcessExpiredOrderHolds_DueMultiTicketOrder_ExpiresOrderAndReleasesEveryReservation()
    {
        var clock = new TestClock { UtcNow = Start };
        await using var factory = CreateFactory(clock);
        await ClearCheckoutDataAsync(factory);
        var guestClient = factory.CreateClient();
        var organizerClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        var data = await SeedPublishedEventWithTwoTicketTypesAsync(
            factory,
            organizerClient,
            firstCapacity: 3,
            firstPriceAmount: 50m,
            secondCapacity: 4,
            secondPriceAmount: 75m);
        var orderId = await PlacePaidOrderAsync(
            guestClient,
            data.EventId,
            [
                new PlaceOrderLineRequest(data.FirstTicketTypeId, 2),
                new PlaceOrderLineRequest(data.SecondTicketTypeId, 1),
            ]);

        clock.UtcNow = Start.AddMinutes(16);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            var result = await sender.Send(new EventHub.Application.Orders.Commands.ProcessExpiredOrderHoldsCommand());

            result.IsSuccess.Should().BeTrue();
            result.Value!.ExpiredOrderCount.Should().Be(1);
            result.Value.ReleasedReservationCount.Should().Be(2);
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
            var order = await databaseContext.Orders.SingleAsync(order => order.Id == orderId);
            var firstTicketType = await databaseContext.TicketTypes
                .SingleAsync(ticketType => ticketType.Id == data.FirstTicketTypeId);
            var secondTicketType = await databaseContext.TicketTypes
                .SingleAsync(ticketType => ticketType.Id == data.SecondTicketTypeId);

            order.Status.Should().Be("Expired");
            order.ReservationId.Should().BeNull();
            firstTicketType.Reserved.Should().Be(0);
            firstTicketType.Sold.Should().Be(0);
            secondTicketType.Reserved.Should().Be(0);
            secondTicketType.Sold.Should().Be(0);
            (await databaseContext.Reservations.CountAsync(reservation => reservation.OrderId == orderId)).Should().Be(0);
        }
    }

    [Fact]
    public async Task ProcessExpiredOrderHolds_MultipleOrdersForSameEvent_ExpiresAndReleasesEveryHold()
    {
        var clock = new TestClock { UtcNow = Start };
        await using var factory = CreateFactory(clock);
        await ClearCheckoutDataAsync(factory);
        var guestClient = factory.CreateClient();
        var organizerClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        var data = await SeedPublishedEventAsync(factory, organizerClient, capacity: 4, priceAmount: 50m);
        var firstOrderId = await PlacePaidOrderAsync(guestClient, data.EventId, data.TicketTypeId, quantity: 1);
        var secondOrderId = await PlacePaidOrderAsync(guestClient, data.EventId, data.TicketTypeId, quantity: 1);

        clock.UtcNow = Start.AddMinutes(16);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            var result = await sender.Send(new EventHub.Application.Orders.Commands.ProcessExpiredOrderHoldsCommand());

            result.IsSuccess.Should().BeTrue();
            result.Value!.ExpiredOrderCount.Should().Be(2);
            result.Value.ReleasedReservationCount.Should().Be(2);
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
            var firstOrder = await databaseContext.Orders.SingleAsync(order => order.Id == firstOrderId);
            var secondOrder = await databaseContext.Orders.SingleAsync(order => order.Id == secondOrderId);
            var ticketType = await databaseContext.TicketTypes.SingleAsync(type => type.Id == data.TicketTypeId);

            firstOrder.Status.Should().Be("Expired");
            secondOrder.Status.Should().Be("Expired");
            ticketType.Reserved.Should().Be(0);
            ticketType.Sold.Should().Be(0);
            (await databaseContext.Reservations.CountAsync(reservation => reservation.EventId == data.EventId)).Should().Be(0);
        }
    }

    [Fact]
    public async Task DispatchOrderExpiredEvent_WithResidualMultiTicketReservations_ReleasesEveryReservation()
    {
        var clock = new TestClock { UtcNow = Start };
        await using var factory = CreateFactory(clock);
        await ClearCheckoutDataAsync(factory);
        var guestClient = factory.CreateClient();
        var organizerClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        var data = await SeedPublishedEventWithTwoTicketTypesAsync(
            factory,
            organizerClient,
            firstCapacity: 3,
            firstPriceAmount: 50m,
            secondCapacity: 4,
            secondPriceAmount: 75m);
        var orderId = await PlacePaidOrderAsync(
            guestClient,
            data.EventId,
            [
                new PlaceOrderLineRequest(data.FirstTicketTypeId, 2),
                new PlaceOrderLineRequest(data.SecondTicketTypeId, 1),
            ]);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
            var storedOrder = await databaseContext.Orders.SingleAsync(storedOrder => storedOrder.Id == orderId);
            storedOrder.Status = "Expired";
            storedOrder.ExpiresAt = Start.AddMinutes(16);
            await databaseContext.SaveChangesAsync();
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();
            await dispatcher.DispatchAsync(
            [
                new OrderExpiredEvent(
                    OrderId.From(orderId),
                    Start.AddMinutes(16)),
            ]);
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
            var storedOrder = await databaseContext.Orders.SingleAsync(storedOrder => storedOrder.Id == orderId);
            var firstTicketType = await databaseContext.TicketTypes
                .SingleAsync(ticketType => ticketType.Id == data.FirstTicketTypeId);
            var secondTicketType = await databaseContext.TicketTypes
                .SingleAsync(ticketType => ticketType.Id == data.SecondTicketTypeId);

            storedOrder.ReservationId.Should().BeNull();
            firstTicketType.Reserved.Should().Be(0);
            firstTicketType.Sold.Should().Be(0);
            secondTicketType.Reserved.Should().Be(0);
            secondTicketType.Sold.Should().Be(0);
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
        => await PlacePaidOrderAsync(
            guestClient,
            eventId,
            [new PlaceOrderLineRequest(ticketTypeId, quantity)]);

    private static async Task<int> PlacePaidOrderAsync(
        HttpClient guestClient,
        int eventId,
        IReadOnlyList<PlaceOrderLineRequest> lines)
    {
        var request = new PlaceOrderRequest(
            "Jane Attendee",
            "jane@example.com",
            lines.ToList());

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

    private static async Task<EventDataWithTwoTicketTypes> SeedPublishedEventWithTwoTicketTypesAsync(
        IntegrationTestWebApplicationFactory factory,
        HttpClient organizerClient,
        int firstCapacity,
        decimal firstPriceAmount,
        int secondCapacity,
        decimal secondPriceAmount)
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

        var firstTicketType = new TicketTypeRecord
        {
            EventId = eventRecord.Id,
            Name = "General Admission",
            PriceAmount = firstPriceAmount,
            PriceCurrency = "VND",
            Capacity = firstCapacity,
            MaxPerOrder = 4,
            Sold = 0,
            Reserved = 0,
            CreatedAt = Start,
            UpdatedAt = Start,
        };
        var secondTicketType = new TicketTypeRecord
        {
            EventId = eventRecord.Id,
            Name = "VIP",
            PriceAmount = secondPriceAmount,
            PriceCurrency = "VND",
            Capacity = secondCapacity,
            MaxPerOrder = 4,
            Sold = 0,
            Reserved = 0,
            CreatedAt = Start,
            UpdatedAt = Start,
        };

        databaseContext.TicketTypes.AddRange(firstTicketType, secondTicketType);
        await databaseContext.SaveChangesAsync();

        return new EventDataWithTwoTicketTypes(eventRecord.Id, firstTicketType.Id, secondTicketType.Id);
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

    private sealed record EventDataWithTwoTicketTypes(
        int EventId,
        int FirstTicketTypeId,
        int SecondTicketTypeId);
}
