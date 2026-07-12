using System.Net;
using System.Net.Http.Json;
using EventHub.Api.Common;
using EventHub.Api.IntegrationTests.Integration;
using EventHub.Contracts.Tickets;
using EventHub.Domain.Events;
using EventHub.Domain.Users;
using EventHub.Infrastructure.Persistence;
using EventHub.Infrastructure.Persistence.Entities;
using EventHub.Testing.Common.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.Api.IntegrationTests.Tickets;

[Collection(IntegrationTestCollection.Name)]
public sealed class ReturnTicketTests(IntegrationTestFixture fixture)
{
    private static readonly DateTimeOffset Now = new(2026, 7, 12, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ReturnTicket_SoldOutBeforeEvent_ReturnsTicketToPool()
    {
        await using var factory = fixture.CreateFactory();
        var client = factory.CreateClient();
        var data = await SeedReturnableTicketAsync(factory, capacity: 1, sold: 1);

        using var response = await client.PostAsync(
            $"/api/orders/{data.OrderId}/tickets/{data.TicketId}/return",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ReturnTicketResponse>();
        body.Should().NotBeNull();
        body!.TicketStatus.Should().Be("void");
        body.OrderStatus.Should().Be("refunded");

        await using var scope = factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var ticket = await databaseContext.Tickets.SingleAsync(ticket => ticket.Id == data.TicketId);
        var order = await databaseContext.Orders.SingleAsync(order => order.Id == data.OrderId);
        var ticketType = await databaseContext.TicketTypes.SingleAsync(ticketType => ticketType.Id == data.TicketTypeId);

        ticket.Status.Should().Be("Void");
        order.Status.Should().Be("Refunded");
        ticketType.Sold.Should().Be(0);
    }

    [Fact]
    public async Task ReturnTicket_WhenTicketTypeIsNotSoldOut_Returns422()
    {
        await using var factory = fixture.CreateFactory();
        var client = factory.CreateClient();
        var data = await SeedReturnableTicketAsync(factory, capacity: 5, sold: 1);

        using var response = await client.PostAsync(
            $"/api/orders/{data.OrderId}/tickets/{data.TicketId}/return",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await response.Content.ReadFromJsonAsync<ApiProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Code.Should().Be("EVENT_NOT_SOLD_OUT");
    }

    private static async Task<ReturnTicketData> SeedReturnableTicketAsync(
        IntegrationTestWebApplicationFactory factory,
        int capacity,
        int sold)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var organizerId = Guid.NewGuid();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        databaseContext.Users.Add(new UserRecord
        {
            Id = organizerId,
            DisplayName = "Return Organizer",
            Email = $"return-organizer-{suffix}@example.com",
            PasswordHash = "hash",
            Role = UserRole.Organizer,
            CreatedAt = Now,
            UpdatedAt = Now,
            RowVersion = 1,
        });

        var eventRecord = new EventRecord
        {
            Title = $"Return Event {suffix}",
            OrganizerId = organizerId,
            ScheduleStartsAt = Now.AddDays(7),
            ScheduleEndsAt = Now.AddDays(7).AddHours(2),
            ScheduleTimeZoneId = "UTC",
            LocationPhysicalAddress = "1 Return Way",
            LocationIsOnline = false,
            Status = EventStatus.Published,
            Slug = $"return-{suffix}",
            CreatedAt = Now,
            UpdatedAt = Now,
        };
        databaseContext.Events.Add(eventRecord);
        await databaseContext.SaveChangesAsync();

        var ticketType = new TicketTypeRecord
        {
            EventId = eventRecord.Id,
            Name = "General Admission",
            PriceAmount = 0m,
            PriceCurrency = "VND",
            Capacity = capacity,
            Sold = sold,
            Reserved = 0,
            CreatedAt = Now,
            UpdatedAt = Now,
        };
        databaseContext.TicketTypes.Add(ticketType);
        await databaseContext.SaveChangesAsync();

        var order = new OrderRecord
        {
            EventId = eventRecord.Id,
            ContactName = "Return Holder",
            ContactEmail = "return-holder@example.com",
            Status = "Confirmed",
            TotalAmount = 0m,
            TotalCurrency = "VND",
            PlacedAt = Now.AddHours(-1),
            ConfirmedAt = Now.AddHours(-1),
            RowVersion = 1,
            Lines =
            [
                new OrderLineRecord
                {
                    TicketTypeId = ticketType.Id,
                    Quantity = 1,
                    UnitPriceAmount = 0m,
                    UnitPriceCurrency = "VND",
                    LineTotalAmount = 0m,
                    LineTotalCurrency = "VND",
                },
            ],
        };
        databaseContext.Orders.Add(order);
        await databaseContext.SaveChangesAsync();

        var ticket = new TicketRecord
        {
            EventId = eventRecord.Id,
            OrderId = order.Id,
            TicketTypeId = ticketType.Id,
            Code = $"tk_{Guid.NewGuid():N}",
            HolderName = "Return Holder",
            HolderEmail = "return-holder@example.com",
            Status = "Valid",
            IssuedAt = Now.AddHours(-1),
            RowVersion = 1,
        };
        databaseContext.Tickets.Add(ticket);
        await databaseContext.SaveChangesAsync();

        return new ReturnTicketData(order.Id, ticket.Id, ticketType.Id);
    }

    private sealed record ReturnTicketData(int OrderId, int TicketId, int TicketTypeId);
}
