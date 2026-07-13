using System.Net;
using System.Net.Http.Json;
using EventHub.Api.IntegrationTests.Integration;
using EventHub.Application.Abstractions.Services;
using EventHub.Contracts.Orders;
using EventHub.Contracts.Payments;
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

namespace EventHub.Api.IntegrationTests.Payments;

[Collection(IntegrationTestCollection.Name)]
public sealed class PaymentTests(IntegrationTestFixture fixture)
{
    private static readonly DateTimeOffset Start = new(2026, 7, 5, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task StartPayment_PaidPendingOrder_ReturnsProviderRedirectAndStoresNoFeeAmount()
    {
        var clock = new TestClock { UtcNow = Start };
        await using var factory = CreateFactory(clock);
        await ClearPaymentDataAsync(factory);
        var guestClient = factory.CreateClient();
        var organizerClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        var data = await SeedPublishedEventAsync(factory, organizerClient, capacity: 3, priceAmount: 50m);
        var order = await PlaceOrderAsync(guestClient, data.EventId, data.TicketTypeId, quantity: 2);

        using var response = await guestClient.PostAsJsonAsync(
            $"/api/orders/{order.OrderId}/payments",
            new StartPaymentRequest("https://example.test/success", "https://example.test/cancel"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var payment = await response.Content.ReadFromJsonAsync<StartPaymentResponse>();
        payment.Should().NotBeNull();
        payment!.Amount.Should().Be(order.TotalAmount);
        payment.Currency.Should().Be(order.TotalCurrency);
        payment.RedirectUrl.Should().Be("https://example.test/success");
        payment.ProviderReference.Should().StartWith($"local-payment-{order.OrderId}-");

        await using var scope = factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var record = await databaseContext.Payments.SingleAsync(record => record.Id == payment.PaymentId);
        record.Amount.Should().Be(order.TotalAmount);
        record.Currency.Should().Be(order.TotalCurrency);
        record.Status.Should().Be("Initiated");
        record.ProviderReference.Should().Be(payment.ProviderReference);
    }

    [Fact]
    public async Task ConfirmPayment_SuccessNotification_ConfirmsOrderAndIsIdempotent()
    {
        var clock = new TestClock { UtcNow = Start };
        await using var factory = CreateFactory(clock);
        await ClearPaymentDataAsync(factory);
        var guestClient = factory.CreateClient();
        var organizerClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        var data = await SeedPublishedEventAsync(factory, organizerClient, capacity: 3, priceAmount: 50m);
        var order = await PlaceOrderAsync(guestClient, data.EventId, data.TicketTypeId, quantity: 2);
        var payment = await StartPaymentAsync(guestClient, order.OrderId);

        using var firstResponse = await guestClient.PostAsJsonAsync(
            "/api/payments/provider-notifications/succeeded",
            new PaymentProviderNotificationRequest(payment.ProviderReference));
        using var secondResponse = await guestClient.PostAsJsonAsync(
            "/api/payments/provider-notifications/succeeded",
            new PaymentProviderNotificationRequest(payment.ProviderReference));

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var first = await firstResponse.Content.ReadFromJsonAsync<PaymentProviderNotificationResponse>();
        var second = await secondResponse.Content.ReadFromJsonAsync<PaymentProviderNotificationResponse>();
        first.Should().NotBeNull();
        second.Should().NotBeNull();
        first!.Applied.Should().BeTrue();
        first.PaymentStatus.Should().Be("captured");
        first.OrderStatus.Should().Be("confirmed");
        second!.Applied.Should().BeFalse();
        second.PaymentStatus.Should().Be("captured");
        second.OrderStatus.Should().Be("confirmed");

        await using var scope = factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var storedOrder = await databaseContext.Orders.SingleAsync(storedOrder => storedOrder.Id == order.OrderId);
        var storedPayment = await databaseContext.Payments.SingleAsync(storedPayment => storedPayment.Id == payment.PaymentId);
        var ticketType = await databaseContext.TicketTypes.SingleAsync(ticketType => ticketType.Id == data.TicketTypeId);

        storedOrder.Status.Should().Be("Confirmed");
        storedOrder.PaymentId.Should().Be(payment.PaymentId);
        storedOrder.ReservationId.Should().BeNull();
        storedPayment.Status.Should().Be("Captured");
        ticketType.Reserved.Should().Be(0);
        ticketType.Sold.Should().Be(2);
        (await databaseContext.Reservations.CountAsync(reservation => reservation.OrderId == order.OrderId)).Should().Be(0);
        (await databaseContext.Tickets.CountAsync(ticket => ticket.OrderId == order.OrderId)).Should().Be(2);
    }

    [Fact]
    public async Task ConfirmPayment_MultiTicketOrder_ConfirmsEveryReservationAndIsIdempotent()
    {
        var clock = new TestClock { UtcNow = Start };
        await using var factory = CreateFactory(clock);
        await ClearPaymentDataAsync(factory);
        var guestClient = factory.CreateClient();
        var organizerClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        var data = await SeedPublishedEventWithTwoTicketTypesAsync(
            factory,
            organizerClient,
            firstCapacity: 3,
            firstPriceAmount: 50m,
            secondCapacity: 4,
            secondPriceAmount: 75m);
        var order = await PlaceOrderAsync(
            guestClient,
            data.EventId,
            [
                new PlaceOrderLineRequest(data.FirstTicketTypeId, 2),
                new PlaceOrderLineRequest(data.SecondTicketTypeId, 1),
            ]);
        var payment = await StartPaymentAsync(guestClient, order.OrderId);

        using var firstResponse = await guestClient.PostAsJsonAsync(
            "/api/payments/provider-notifications/succeeded",
            new PaymentProviderNotificationRequest(payment.ProviderReference));
        using var secondResponse = await guestClient.PostAsJsonAsync(
            "/api/payments/provider-notifications/succeeded",
            new PaymentProviderNotificationRequest(payment.ProviderReference));

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var first = await firstResponse.Content.ReadFromJsonAsync<PaymentProviderNotificationResponse>();
        var second = await secondResponse.Content.ReadFromJsonAsync<PaymentProviderNotificationResponse>();
        first.Should().NotBeNull();
        second.Should().NotBeNull();
        first!.Applied.Should().BeTrue();
        second!.Applied.Should().BeFalse();

        await using var scope = factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var storedOrder = await databaseContext.Orders.SingleAsync(storedOrder => storedOrder.Id == order.OrderId);
        var firstTicketType = await databaseContext.TicketTypes
            .SingleAsync(ticketType => ticketType.Id == data.FirstTicketTypeId);
        var secondTicketType = await databaseContext.TicketTypes
            .SingleAsync(ticketType => ticketType.Id == data.SecondTicketTypeId);

        storedOrder.Status.Should().Be("Confirmed");
        storedOrder.ReservationId.Should().BeNull();
        firstTicketType.Reserved.Should().Be(0);
        firstTicketType.Sold.Should().Be(2);
        secondTicketType.Reserved.Should().Be(0);
        secondTicketType.Sold.Should().Be(1);
        (await databaseContext.Reservations.CountAsync(reservation => reservation.OrderId == order.OrderId)).Should().Be(0);
        (await databaseContext.Tickets.CountAsync(ticket => ticket.OrderId == order.OrderId)).Should().Be(3);
    }

    [Fact]
    public async Task DispatchOrderConfirmedEvent_WithResidualMultiTicketReservations_CommitsEveryReservation()
    {
        var clock = new TestClock { UtcNow = Start };
        await using var factory = CreateFactory(clock);
        await ClearPaymentDataAsync(factory);
        var guestClient = factory.CreateClient();
        var organizerClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        var data = await SeedPublishedEventWithTwoTicketTypesAsync(
            factory,
            organizerClient,
            firstCapacity: 3,
            firstPriceAmount: 50m,
            secondCapacity: 4,
            secondPriceAmount: 75m);
        var order = await PlaceOrderAsync(
            guestClient,
            data.EventId,
            [
                new PlaceOrderLineRequest(data.FirstTicketTypeId, 2),
                new PlaceOrderLineRequest(data.SecondTicketTypeId, 1),
            ]);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
            var storedOrder = await databaseContext.Orders.SingleAsync(storedOrder => storedOrder.Id == order.OrderId);
            storedOrder.Status = "Confirmed";
            storedOrder.ConfirmedAt = Start;
            await databaseContext.SaveChangesAsync();
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();
            await dispatcher.DispatchAsync(
            [
                new OrderConfirmedEvent(
                    OrderId.From(order.OrderId),
                    EventId.From(data.EventId),
                    Start),
            ]);
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
            var storedOrder = await databaseContext.Orders.SingleAsync(storedOrder => storedOrder.Id == order.OrderId);
            var firstTicketType = await databaseContext.TicketTypes
                .SingleAsync(ticketType => ticketType.Id == data.FirstTicketTypeId);
            var secondTicketType = await databaseContext.TicketTypes
                .SingleAsync(ticketType => ticketType.Id == data.SecondTicketTypeId);

            storedOrder.ReservationId.Should().BeNull();
            firstTicketType.Reserved.Should().Be(0);
            firstTicketType.Sold.Should().Be(2);
            secondTicketType.Reserved.Should().Be(0);
            secondTicketType.Sold.Should().Be(1);
            (await databaseContext.Reservations.CountAsync(reservation => reservation.OrderId == order.OrderId)).Should().Be(0);
            (await databaseContext.Tickets.CountAsync(ticket => ticket.OrderId == order.OrderId)).Should().Be(3);
        }
    }

    [Fact]
    public async Task FailPayment_FailureNotification_LeavesOrderPendingUntilHoldExpiry()
    {
        var clock = new TestClock { UtcNow = Start };
        await using var factory = CreateFactory(clock);
        await ClearPaymentDataAsync(factory);
        var guestClient = factory.CreateClient();
        var organizerClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        var data = await SeedPublishedEventAsync(factory, organizerClient, capacity: 3, priceAmount: 50m);
        var order = await PlaceOrderAsync(guestClient, data.EventId, data.TicketTypeId, quantity: 2);
        var payment = await StartPaymentAsync(guestClient, order.OrderId);

        using var failResponse = await guestClient.PostAsJsonAsync(
            "/api/payments/provider-notifications/failed",
            new PaymentProviderNotificationRequest(payment.ProviderReference));

        failResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var notification = await failResponse.Content.ReadFromJsonAsync<PaymentProviderNotificationResponse>();
        notification.Should().NotBeNull();
        notification!.Applied.Should().BeTrue();
        notification.PaymentStatus.Should().Be("failed");
        notification.OrderStatus.Should().Be("pending");

        clock.UtcNow = Start.AddMinutes(16);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            var result = await sender.Send(new EventHub.Application.Orders.Commands.ProcessExpiredOrderHoldsCommand());
            result.IsSuccess.Should().BeTrue();
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
            var storedOrder = await databaseContext.Orders.SingleAsync(storedOrder => storedOrder.Id == order.OrderId);
            var storedPayment = await databaseContext.Payments.SingleAsync(storedPayment => storedPayment.Id == payment.PaymentId);
            var ticketType = await databaseContext.TicketTypes.SingleAsync(ticketType => ticketType.Id == data.TicketTypeId);

            storedOrder.Status.Should().Be("Expired");
            storedPayment.Status.Should().Be("Failed");
            ticketType.Reserved.Should().Be(0);
        }
    }

    [Fact]
    public async Task StartPayment_FreeConfirmedOrder_ReturnsValidationError()
    {
        var clock = new TestClock { UtcNow = Start };
        await using var factory = CreateFactory(clock);
        await ClearPaymentDataAsync(factory);
        var guestClient = factory.CreateClient();
        var organizerClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        var data = await SeedPublishedEventAsync(factory, organizerClient, capacity: 3, priceAmount: 0m);
        var order = await PlaceOrderAsync(guestClient, data.EventId, data.TicketTypeId, quantity: 2);

        order.Status.Should().Be("confirmed");

        using var response = await guestClient.PostAsJsonAsync(
            $"/api/orders/{order.OrderId}/payments",
            new StartPaymentRequest("https://example.test/success", "https://example.test/cancel"));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    private IntegrationTestWebApplicationFactory CreateFactory(TestClock clock) =>
        fixture.CreateFactory(services =>
        {
            services.RemoveAll<IClock>();
            services.AddSingleton<IClock>(clock);
            services.RemoveAll<IHostedService>();
        });

    private static async Task ClearPaymentDataAsync(IntegrationTestWebApplicationFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        await databaseContext.Payments.ExecuteDeleteAsync();
        await databaseContext.Reservations.ExecuteDeleteAsync();
        await databaseContext.OrderLines.ExecuteDeleteAsync();
        await databaseContext.Orders.ExecuteDeleteAsync();
    }

    private static async Task<PlaceOrderResponse> PlaceOrderAsync(
        HttpClient guestClient,
        int eventId,
        int ticketTypeId,
        int quantity)
        => await PlaceOrderAsync(
            guestClient,
            eventId,
            [new PlaceOrderLineRequest(ticketTypeId, quantity)]);

    private static async Task<PlaceOrderResponse> PlaceOrderAsync(
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
        return body!;
    }

    private static async Task<StartPaymentResponse> StartPaymentAsync(HttpClient guestClient, int orderId)
    {
        using var response = await guestClient.PostAsJsonAsync(
            $"/api/orders/{orderId}/payments",
            new StartPaymentRequest("https://example.test/success", "https://example.test/cancel"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<StartPaymentResponse>();
        body.Should().NotBeNull();
        return body!;
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
            Title = $"Payment Event {suffix}",
            OrganizerId = organizerId,
            ScheduleStartsAt = new DateTimeOffset(2026, 7, 15, 14, 0, 0, TimeSpan.Zero),
            ScheduleEndsAt = new DateTimeOffset(2026, 7, 15, 16, 0, 0, TimeSpan.Zero),
            ScheduleTimeZoneId = "UTC",
            LocationPhysicalAddress = "123 Payment Ave",
            LocationIsOnline = false,
            Status = EventStatus.Published,
            Slug = $"payment-{suffix}",
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
            Title = $"Payment Event {suffix}",
            OrganizerId = organizerId,
            ScheduleStartsAt = new DateTimeOffset(2026, 7, 15, 14, 0, 0, TimeSpan.Zero),
            ScheduleEndsAt = new DateTimeOffset(2026, 7, 15, 16, 0, 0, TimeSpan.Zero),
            ScheduleTimeZoneId = "UTC",
            LocationPhysicalAddress = "123 Payment Ave",
            LocationIsOnline = false,
            Status = EventStatus.Published,
            Slug = $"payment-{suffix}",
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
        var email = $"payment_{suffix}@example.com";
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
