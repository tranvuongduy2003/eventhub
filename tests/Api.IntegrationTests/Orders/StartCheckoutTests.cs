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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.Api.IntegrationTests.Orders;

[Collection(IntegrationTestCollection.Name)]
public sealed class StartCheckoutTests(IntegrationTestFixture fixture)
{
    private sealed record EventData(string Slug, int EventId, int GeneralId, int VipId);

    private readonly HttpClient _client = fixture.Factory.CreateClient(
        new WebApplicationFactoryClientOptions { HandleCookies = true });

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task StartCheckout_ValidMultipleTicketTypes_ReturnsPriceSnapshotsWithoutCreatingHold()
    {
        var data = await SeedPublishedEventAsync(
            generalCapacity: 10,
            vipCapacity: 5,
            generalMaxPerOrder: 4,
            vipMaxPerOrder: 2);

        var request = new StartCheckoutRequest(
            [
                new StartCheckoutLineRequest(data.GeneralId, 2),
                new StartCheckoutLineRequest(data.VipId, 1),
            ]);

        using var response = await _client.PostAsJsonAsync(
            $"/api/events/{data.Slug}/checkout/start",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<StartCheckoutResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.EventSlug.Should().Be(data.Slug);
        result.Lines.Should().HaveCount(2);
        result.Lines.Should().Contain(l =>
            l.TicketTypeId == data.GeneralId
            && l.Quantity == 2
            && l.UnitPriceAmount == 50m
            && l.LineTotalAmount == 100m);
        result.Lines.Should().Contain(l =>
            l.TicketTypeId == data.VipId
            && l.Quantity == 1
            && l.UnitPriceAmount == 150m
            && l.LineTotalAmount == 150m);
        result.TotalAmount.Should().Be(250m);

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        (await databaseContext.Orders.CountAsync(order => order.EventId == data.EventId)).Should().Be(0);
        (await databaseContext.Reservations.CountAsync(reservation => reservation.EventId == data.EventId))
            .Should()
            .Be(0);
    }

    [Fact]
    public async Task StartCheckout_NoTicketsSelected_Returns422()
    {
        var data = await SeedPublishedEventAsync();
        var request = new StartCheckoutRequest([]);

        using var response = await _client.PostAsJsonAsync(
            $"/api/events/{data.Slug}/checkout/start",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task StartCheckout_QuantityExceedsAvailability_Returns422()
    {
        var data = await SeedPublishedEventAsync(generalCapacity: 3, generalSold: 2, generalReserved: 1);
        var request = new StartCheckoutRequest([new StartCheckoutLineRequest(data.GeneralId, 1)]);

        using var response = await _client.PostAsJsonAsync(
            $"/api/events/{data.Slug}/checkout/start",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var problem = await ReadProblemAsync(response);
        problem.GetProperty("code").GetString().Should().Be("CHECKOUT_TICKET_TYPE_SOLD_OUT");
    }

    [Fact]
    public async Task StartCheckout_QuantityExceedsMaxPerOrder_Returns422()
    {
        var data = await SeedPublishedEventAsync(generalMaxPerOrder: 2);
        var request = new StartCheckoutRequest([new StartCheckoutLineRequest(data.GeneralId, 3)]);

        using var response = await _client.PostAsJsonAsync(
            $"/api/events/{data.Slug}/checkout/start",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var problem = await ReadProblemAsync(response);
        problem.GetProperty("code").GetString().Should().Be("ORDER_MAX_PER_ORDER_EXCEEDED");
    }

    [Fact]
    public async Task StartCheckout_SalesWindowClosed_Returns422()
    {
        var data = await SeedPublishedEventAsync(
            generalSalesWindowStart: DateTimeOffset.UtcNow.AddDays(-3),
            generalSalesWindowEnd: DateTimeOffset.UtcNow.AddDays(-1));
        var request = new StartCheckoutRequest([new StartCheckoutLineRequest(data.GeneralId, 1)]);

        using var response = await _client.PostAsJsonAsync(
            $"/api/events/{data.Slug}/checkout/start",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var problem = await ReadProblemAsync(response);
        problem.GetProperty("code").GetString().Should().Be("CHECKOUT_TICKET_TYPE_SALES_ENDED");
    }

    [Fact]
    public async Task StartCheckout_ClosedEvent_Returns422()
    {
        var data = await SeedPublishedEventAsync(status: EventStatus.Closed);
        var request = new StartCheckoutRequest([new StartCheckoutLineRequest(data.GeneralId, 1)]);

        using var response = await _client.PostAsJsonAsync(
            $"/api/events/{data.Slug}/checkout/start",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var problem = await ReadProblemAsync(response);
        problem.GetProperty("code").GetString().Should().Be("CHECKOUT_EVENT_NOT_PURCHASABLE");
    }

    [Fact]
    public async Task StartCheckout_AnonymousBuyer_Returns200()
    {
        using var unauthenticatedClient = fixture.Factory.CreateClient();
        var data = await SeedPublishedEventAsync();
        var request = new StartCheckoutRequest([new StartCheckoutLineRequest(data.GeneralId, 1)]);

        using var response = await unauthenticatedClient.PostAsJsonAsync(
            $"/api/events/{data.Slug}/checkout/start",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task<JsonElement> ReadProblemAsync(HttpResponseMessage response)
    {
        var responseBody = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<JsonElement>(responseBody, JsonOptions);
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

    private async Task<EventData> SeedPublishedEventAsync(
        EventStatus status = EventStatus.Published,
        int generalCapacity = 10,
        int generalSold = 0,
        int generalReserved = 0,
        int vipCapacity = 5,
        int generalMaxPerOrder = 4,
        int vipMaxPerOrder = 2,
        DateTimeOffset? generalSalesWindowStart = null,
        DateTimeOffset? generalSalesWindowEnd = null)
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
            Status = status,
            Slug = $"tech-conf-{suffix}",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        databaseContext.Events.Add(eventRecord);
        await databaseContext.SaveChangesAsync();

        var general = new TicketTypeRecord
        {
            EventId = eventRecord.Id,
            Name = "General Admission",
            PriceAmount = 50m,
            PriceCurrency = "VND",
            Capacity = generalCapacity,
            MaxPerOrder = generalMaxPerOrder,
            SalesWindowStart = generalSalesWindowStart,
            SalesWindowEnd = generalSalesWindowEnd,
            Sold = generalSold,
            Reserved = generalReserved,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        var vip = new TicketTypeRecord
        {
            EventId = eventRecord.Id,
            Name = "VIP",
            PriceAmount = 150m,
            PriceCurrency = "VND",
            Capacity = vipCapacity,
            MaxPerOrder = vipMaxPerOrder,
            Sold = 0,
            Reserved = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        databaseContext.TicketTypes.AddRange(general, vip);
        await databaseContext.SaveChangesAsync();

        return new EventData(eventRecord.Slug, eventRecord.Id, general.Id, vip.Id);
    }
}
