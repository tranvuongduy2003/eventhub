using System.Net;
using System.Net.Http.Json;
using EventHub.Api.Common;
using EventHub.Api.IntegrationTests.Integration;
using EventHub.Application.Abstractions.Services;
using EventHub.Contracts.Tickets;
using EventHub.Contracts.Users;
using EventHub.Domain.Events;
using EventHub.Domain.Orders;
using EventHub.Infrastructure.Persistence;
using EventHub.Infrastructure.Persistence.Entities;
using EventHub.Testing.Common.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace EventHub.Api.IntegrationTests.Tickets;

[Collection(IntegrationTestCollection.Name)]
public sealed class CheckInTests(IntegrationTestFixture fixture)
{
    private static readonly DateTimeOffset Now = new(2026, 7, 10, 18, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task CheckInByCode_StaffWithCheckInPermission_MarksTicketCheckedIn()
    {
        await using var factory = CreateFactory();
        var staffClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var staffId = await RegisterUserAsync(staffClient, "checkin-staff");
        var data = await SeedDoorDataAsync(factory, staffId, staffId, EventRole.Staff, OrderStatus.Confirmed);

        using var response = await staffClient.PostAsJsonAsync(
            $"/api/events/{data.EventId}/check-ins/scan",
            new CheckInTicketRequest(data.Code));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CheckInTicketResponse>();
        body.Should().NotBeNull();
        body!.Status.Should().Be("checkedin");
        body.CheckedInAt.Should().Be(Now);

        await using var scope = factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var ticket = await databaseContext.Tickets.SingleAsync(ticket => ticket.Id == data.TicketId);
        ticket.Status.Should().Be("CheckedIn");
        ticket.CheckedInAt.Should().Be(Now);
    }

    [Fact]
    public async Task CheckInByCode_UserWithoutCheckInPermission_Returns403()
    {
        await using var factory = CreateFactory();
        var callerClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var ownerClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var callerId = await RegisterUserAsync(callerClient, "checkin-norole");
        var ownerId = await RegisterUserAsync(ownerClient, "checkin-owner");
        var data = await SeedDoorDataAsync(factory, ownerId, callerId, role: null, OrderStatus.Confirmed);

        using var response = await callerClient.PostAsJsonAsync(
            $"/api/events/{data.EventId}/check-ins/scan",
            new CheckInTicketRequest(data.Code));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var problem = await response.Content.ReadFromJsonAsync<ApiProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Code.Should().Be("INSUFFICIENT_PERMISSIONS");
    }

    [Fact]
    public async Task CheckInByCode_WhenCodeBelongsToDifferentEvent_ReturnsClearReason()
    {
        await using var factory = CreateFactory();
        var staffClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var staffId = await RegisterUserAsync(staffClient, "checkin-wrongevent");
        var data = await SeedDoorDataAsync(factory, staffId, staffId, EventRole.Staff, OrderStatus.Confirmed);
        var otherEventId = await SeedPublishedEventAsync(factory, staffId);

        using var response = await staffClient.PostAsJsonAsync(
            $"/api/events/{otherEventId}/check-ins/scan",
            new CheckInTicketRequest(data.Code));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await response.Content.ReadFromJsonAsync<ApiProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Code.Should().Be("TICKET_WRONG_EVENT");
    }

    [Fact]
    public async Task CheckInByCode_WhenCodeIsUnknown_Returns404()
    {
        await using var factory = CreateFactory();
        var staffClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var staffId = await RegisterUserAsync(staffClient, "checkin-unknown");
        var eventId = await SeedPublishedEventAsync(factory, staffId);

        using var response = await staffClient.PostAsJsonAsync(
            $"/api/events/{eventId}/check-ins/scan",
            new CheckInTicketRequest(NewCode()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ApiProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Code.Should().Be("TICKET_NOT_FOUND");
    }

    [Fact]
    public async Task CheckInByCode_WhenOrderIsCancelled_ReturnsClearReason()
    {
        await using var factory = CreateFactory();
        var staffClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var staffId = await RegisterUserAsync(staffClient, "checkin-cancelled");
        var data = await SeedDoorDataAsync(factory, staffId, staffId, EventRole.Staff, OrderStatus.Cancelled);

        using var response = await staffClient.PostAsJsonAsync(
            $"/api/events/{data.EventId}/check-ins/scan",
            new CheckInTicketRequest(data.Code));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await response.Content.ReadFromJsonAsync<ApiProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Code.Should().Be("TICKET_ORDER_NOT_CONFIRMED");
    }

    [Fact]
    public async Task CheckInByCode_WhenTicketAlreadyCheckedIn_ReturnsFirstCheckInTime()
    {
        await using var factory = CreateFactory();
        var staffClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var staffId = await RegisterUserAsync(staffClient, "checkin-duplicate");
        var data = await SeedDoorDataAsync(factory, staffId, staffId, EventRole.Staff, OrderStatus.Confirmed);

        using var firstResponse = await staffClient.PostAsJsonAsync(
            $"/api/events/{data.EventId}/check-ins/scan",
            new CheckInTicketRequest(data.Code));
        using var secondResponse = await staffClient.PostAsJsonAsync(
            $"/api/events/{data.EventId}/check-ins/scan",
            new CheckInTicketRequest(data.Code));

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await secondResponse.Content.ReadFromJsonAsync<ApiProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Code.Should().Be("TICKET_ALREADY_CHECKED_IN");
        problem.Detail.Should().Contain(Now.ToString("O"));
    }

    [Fact]
    public async Task SearchAndManualCheckIn_ByBuyerEmail_ChecksInMatchingTicket()
    {
        await using var factory = CreateFactory();
        var staffClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var staffId = await RegisterUserAsync(staffClient, "checkin-manual");
        var data = await SeedDoorDataAsync(factory, staffId, staffId, EventRole.Staff, OrderStatus.Confirmed);

        using var searchResponse = await staffClient.GetAsync(
            $"/api/events/{data.EventId}/check-ins/tickets?query=buyer%40example.com");

        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var search = await searchResponse.Content.ReadFromJsonAsync<SearchCheckInTicketsResponse>();
        search.Should().NotBeNull();
        search!.Tickets.Should().ContainSingle(ticket => ticket.TicketId == data.TicketId);

        using var checkInResponse = await staffClient.PostAsync(
            $"/api/events/{data.EventId}/check-ins/tickets/{data.TicketId}",
            content: null);

        checkInResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var checkedIn = await checkInResponse.Content.ReadFromJsonAsync<CheckInTicketResponse>();
        checkedIn.Should().NotBeNull();
        checkedIn!.CheckedInAt.Should().Be(Now);
    }

    [Fact]
    public async Task DoorCounts_RequirePermissionAndReturnCheckedInVersusIssued()
    {
        await using var factory = CreateFactory();
        var staffClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var anonymousClient = factory.CreateClient();
        var staffId = await RegisterUserAsync(staffClient, "checkin-counts");
        var data = await SeedDoorDataAsync(factory, staffId, staffId, EventRole.Staff, OrderStatus.Confirmed);

        using var checkInResponse = await staffClient.PostAsJsonAsync(
            $"/api/events/{data.EventId}/check-ins/scan",
            new CheckInTicketRequest(data.Code));
        checkInResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var countsResponse = await staffClient.GetAsync($"/api/events/{data.EventId}/check-ins/counts");
        using var unauthorizedResponse = await anonymousClient.GetAsync($"/api/events/{data.EventId}/check-ins/counts");

        countsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var counts = await countsResponse.Content.ReadFromJsonAsync<DoorCountsResponse>();
        counts.Should().NotBeNull();
        counts!.CheckedIn.Should().Be(1);
        counts.TotalIssued.Should().Be(1);
        unauthorizedResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private IntegrationTestWebApplicationFactory CreateFactory() =>
        fixture.CreateFactory(services =>
        {
            services.RemoveAll<IClock>();
            services.AddSingleton<IClock>(new TestClock { UtcNow = Now });
            services.RemoveAll<IHostedService>();
        });

    private static async Task<Guid> RegisterUserAsync(HttpClient client, string suffix)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/users",
            new RegisterUserRequest(
                $"Check-in {suffix}",
                $"{suffix}_{Guid.NewGuid():N}@example.com",
                "SecurePass1!"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<UserRegistrationResponse>();
        body.Should().NotBeNull();
        return body!.UserId;
    }

    private static async Task<DoorData> SeedDoorDataAsync(
        IntegrationTestWebApplicationFactory factory,
        Guid organizerId,
        Guid? roleUserId,
        EventRole? role,
        OrderStatus orderStatus)
    {
        var eventId = await SeedPublishedEventAsync(factory, organizerId);

        await using var scope = factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var ticketTypeId = await databaseContext.TicketTypes
            .Where(ticketType => ticketType.EventId == eventId)
            .Select(ticketType => ticketType.Id)
            .SingleAsync();

        if (role is not null)
        {
            databaseContext.EventUserRoles.Add(new EventUserRoleRecord
            {
                EventId = eventId,
                UserId = roleUserId!.Value,
                Role = role.Value,
                CreatedAt = Now,
            });
        }

        var order = new OrderRecord
        {
            EventId = eventId,
            ContactName = "Buyer Example",
            ContactEmail = "buyer@example.com",
            Status = orderStatus.ToString(),
            TotalAmount = 0m,
            TotalCurrency = "VND",
            PlacedAt = Now,
            ConfirmedAt = orderStatus == OrderStatus.Confirmed ? Now : null,
            CancelledAt = orderStatus == OrderStatus.Cancelled ? Now : null,
            RowVersion = 1,
            Lines =
            [
                new OrderLineRecord
                {
                    TicketTypeId = ticketTypeId,
                    Quantity = 1,
                    UnitPriceAmount = 0m,
                    UnitPriceCurrency = "VND",
                    LineTotalAmount = 0m,
                    LineTotalCurrency = "VND",
                }
            ],
        };
        databaseContext.Orders.Add(order);
        await databaseContext.SaveChangesAsync();

        var code = NewCode();
        var ticket = new TicketRecord
        {
            EventId = eventId,
            OrderId = order.Id,
            TicketTypeId = ticketTypeId,
            Code = code,
            HolderName = "Buyer Example",
            HolderEmail = "buyer@example.com",
            Status = "Valid",
            IssuedAt = Now.AddHours(-1),
            RowVersion = 1,
        };
        databaseContext.Tickets.Add(ticket);
        await databaseContext.SaveChangesAsync();

        return new DoorData(eventId, ticket.Id, code);
    }

    private static async Task<int> SeedPublishedEventAsync(
        IntegrationTestWebApplicationFactory factory,
        Guid organizerId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var eventRecord = new EventRecord
        {
            Title = $"Check-in Event {suffix}",
            OrganizerId = organizerId,
            ScheduleStartsAt = new DateTimeOffset(2026, 7, 15, 14, 0, 0, TimeSpan.Zero),
            ScheduleEndsAt = new DateTimeOffset(2026, 7, 15, 16, 0, 0, TimeSpan.Zero),
            ScheduleTimeZoneId = "UTC",
            LocationPhysicalAddress = "123 Door Ave",
            LocationIsOnline = false,
            Status = EventStatus.Published,
            Slug = $"check-in-{suffix}",
            CreatedAt = Now,
            UpdatedAt = Now,
        };
        databaseContext.Events.Add(eventRecord);
        await databaseContext.SaveChangesAsync();

        databaseContext.TicketTypes.Add(new TicketTypeRecord
        {
            EventId = eventRecord.Id,
            Name = "General Admission",
            PriceAmount = 0m,
            PriceCurrency = "VND",
            Capacity = 10,
            MaxPerOrder = 4,
            Sold = 1,
            Reserved = 0,
            CreatedAt = Now,
            UpdatedAt = Now,
        });
        await databaseContext.SaveChangesAsync();

        return eventRecord.Id;
    }

    private static string NewCode() => $"tk_{Guid.NewGuid():N}";

    private sealed record DoorData(int EventId, int TicketId, string Code);
}
