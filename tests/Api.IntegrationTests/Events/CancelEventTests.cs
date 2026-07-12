using System.Net;
using System.Net.Http.Json;
using EventHub.Api.IntegrationTests.Integration;
using EventHub.Contracts.Events;
using EventHub.Contracts.Users;
using EventHub.Domain.Events;
using EventHub.Infrastructure.Persistence;
using EventHub.Infrastructure.Persistence.Entities;
using EventHub.Testing.Common.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.Api.IntegrationTests.Events;

[Collection(IntegrationTestCollection.Name)]
public sealed class CancelEventTests(IntegrationTestFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient(
        new WebApplicationFactoryClientOptions { HandleCookies = true });

    [Fact]
    public async Task CancelEvent_PublishedEvent_Returns200WithCancelledStatus()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedPublishedEventAsync(userId);

        using var response = await _client.PostAsync($"/api/events/{eventId}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CancelEventResponse>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("Cancelled");
        result.CancelledAt.Should().NotBe(default);
    }

    [Fact]
    public async Task CancelEvent_ClosedEvent_Returns200WithCancelledStatus()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedClosedEventAsync(userId);

        using var response = await _client.PostAsync($"/api/events/{eventId}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CancelEventResponse>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task CancelEvent_DraftEvent_Returns422()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId);

        using var response = await _client.PostAsync($"/api/events/{eventId}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CancelEvent_AlreadyCancelledEvent_Returns422()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedPublishedEventAsync(userId);

        using var firstCancel = await _client.PostAsync($"/api/events/{eventId}/cancel", null);
        firstCancel.StatusCode.Should().Be(HttpStatusCode.OK);

        using var secondCancel = await _client.PostAsync($"/api/events/{eventId}/cancel", null);

        secondCancel.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CancelEvent_NonExistentEvent_Returns403()
    {
        await RegisterOrganizerAsync();

        using var response = await _client.PostAsync("/api/events/99999/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CancelEvent_NoAuth_Returns401()
    {
        using var response = await _client.PostAsync("/api/events/1/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CancelEvent_NonOwner_Returns403()
    {
        var ownerId = await RegisterOrganizerAsync();
        var eventId = await SeedPublishedEventAsync(ownerId);

        await RegisterOrganizerAsync();

        using var response = await _client.PostAsync($"/api/events/{eventId}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CancelEvent_PublishedEvent_EventDetailsShowCancelledStatus()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedPublishedEventAsync(userId);

        using var cancelResponse = await _client.PostAsync($"/api/events/{eventId}/cancel", null);
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var getResponse = await _client.GetAsync($"/api/events/{eventId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var details = await getResponse.Content.ReadFromJsonAsync<EventDetailsResponse>();
        details.Should().NotBeNull();
        details!.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task CancelEvent_WithCapturedPaidOrder_RefundsOrderPaymentAndVoidsTickets()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedPublishedEventAsync(userId);
        var data = await SeedCapturedPaidOrderAsync(eventId);

        using var response = await _client.PostAsync($"/api/events/{eventId}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var order = await databaseContext.Orders.SingleAsync(order => order.Id == data.OrderId);
        var payment = await databaseContext.Payments.SingleAsync(payment => payment.Id == data.PaymentId);
        var ticket = await databaseContext.Tickets.SingleAsync(ticket => ticket.Id == data.TicketId);

        order.Status.Should().Be("Refunded");
        payment.Status.Should().Be("Refunded");
        payment.RefundedAt.Should().NotBeNull();
        ticket.Status.Should().Be("Void");
    }

    [Fact]
    public async Task CancelEvent_ClosedEvent_EventDetailsShowCancelledStatus()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedClosedEventAsync(userId);

        using var cancelResponse = await _client.PostAsync($"/api/events/{eventId}/cancel", null);
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var getResponse = await _client.GetAsync($"/api/events/{eventId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var details = await getResponse.Content.ReadFromJsonAsync<EventDetailsResponse>();
        details.Should().NotBeNull();
        details!.Status.Should().Be("Cancelled");
    }

    private async Task<Guid> RegisterOrganizerAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var request = new RegisterUserRequest(
            $"Organizer {suffix}",
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

    private async Task<int> SeedPublishedEventAsync(Guid organizerId)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var eventRecord = new EventRecord
        {
            Title = $"Published Conference {suffix}",
            OrganizerId = organizerId,
            ScheduleStartsAt = new DateTimeOffset(2026, 7, 15, 14, 0, 0, TimeSpan.Zero),
            ScheduleEndsAt = new DateTimeOffset(2026, 7, 15, 16, 0, 0, TimeSpan.Zero),
            ScheduleTimeZoneId = "UTC",
            LocationPhysicalAddress = "123 Conference Ave",
            LocationIsOnline = false,
            Status = EventStatus.Published,
            Slug = $"published-conf-{suffix}",
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

    private async Task<int> SeedClosedEventAsync(Guid organizerId)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var eventRecord = new EventRecord
        {
            Title = $"Closed Conference {suffix}",
            OrganizerId = organizerId,
            ScheduleStartsAt = new DateTimeOffset(2026, 7, 15, 14, 0, 0, TimeSpan.Zero),
            ScheduleEndsAt = new DateTimeOffset(2026, 7, 15, 16, 0, 0, TimeSpan.Zero),
            ScheduleTimeZoneId = "UTC",
            LocationPhysicalAddress = "123 Conference Ave",
            LocationIsOnline = false,
            Status = EventStatus.Closed,
            Slug = $"closed-conf-{suffix}",
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

    private async Task<PaidOrderData> SeedCapturedPaidOrderAsync(int eventId)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        var ticketType = new TicketTypeRecord
        {
            EventId = eventId,
            Name = $"Paid Admission {Guid.NewGuid():N}"[..20],
            PriceAmount = 25m,
            PriceCurrency = "VND",
            Capacity = 10,
            Sold = 1,
            Reserved = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        databaseContext.TicketTypes.Add(ticketType);
        await databaseContext.SaveChangesAsync();

        var order = new OrderRecord
        {
            EventId = eventId,
            ContactName = "Paid Buyer",
            ContactEmail = "paid-buyer@example.com",
            Status = "Confirmed",
            TotalAmount = 25m,
            TotalCurrency = "VND",
            PlacedAt = DateTimeOffset.UtcNow.AddMinutes(-30),
            ConfirmedAt = DateTimeOffset.UtcNow.AddMinutes(-20),
            RowVersion = 1,
            Lines =
            [
                new OrderLineRecord
                {
                    TicketTypeId = ticketType.Id,
                    Quantity = 1,
                    UnitPriceAmount = 25m,
                    UnitPriceCurrency = "VND",
                    LineTotalAmount = 25m,
                    LineTotalCurrency = "VND",
                },
            ],
        };
        databaseContext.Orders.Add(order);
        await databaseContext.SaveChangesAsync();

        var payment = new PaymentRecord
        {
            OrderId = order.Id,
            Amount = 25m,
            Currency = "VND",
            Status = "Captured",
            ProviderReference = $"local-payment-{order.Id}-{Guid.NewGuid():N}",
            InitiatedAt = DateTimeOffset.UtcNow.AddMinutes(-25),
            CapturedAt = DateTimeOffset.UtcNow.AddMinutes(-20),
            RowVersion = 1,
        };
        databaseContext.Payments.Add(payment);
        await databaseContext.SaveChangesAsync();

        order.PaymentId = payment.Id;
        databaseContext.Orders.Update(order);

        var ticket = new TicketRecord
        {
            EventId = eventId,
            OrderId = order.Id,
            TicketTypeId = ticketType.Id,
            Code = $"tk_{Guid.NewGuid():N}",
            HolderName = "Paid Buyer",
            HolderEmail = "paid-buyer@example.com",
            Status = "Valid",
            IssuedAt = DateTimeOffset.UtcNow.AddMinutes(-19),
            RowVersion = 1,
        };
        databaseContext.Tickets.Add(ticket);
        await databaseContext.SaveChangesAsync();

        return new PaidOrderData(order.Id, payment.Id, ticket.Id);
    }

    private sealed record PaidOrderData(int OrderId, int PaymentId, int TicketId);
}
