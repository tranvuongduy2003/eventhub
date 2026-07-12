using System.Net;
using System.Net.WebSockets;
using System.Net.Http.Json;
using EventHub.Api.IntegrationTests.Integration;
using EventHub.Application.Realtime;
using EventHub.Contracts.Orders;
using EventHub.Contracts.Users;
using EventHub.Domain.Events;
using EventHub.Infrastructure.Persistence;
using EventHub.Infrastructure.Persistence.Entities;
using EventHub.Testing.Common.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EventHub.Api.IntegrationTests.Realtime;

[Collection(IntegrationTestCollection.Name)]
public sealed class LiveSalesInventoryTests(IntegrationTestFixture fixture)
{
    private static readonly DateTimeOffset Now = new(2026, 7, 12, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task FreeOrderConfirmation_NotifiesSalesInventoryAfterCommit()
    {
        var notifier = new RecordingRealtimeSalesInventoryNotifier();
        await using var factory = fixture.CreateFactory(services =>
        {
            services.RemoveAll<IRealtimeSalesInventoryNotifier>();
            services.AddSingleton<IRealtimeSalesInventoryNotifier>(notifier);
        });
        var organizerClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var guestClient = factory.CreateClient();
        var organizerId = await RegisterOrganizerAsync(organizerClient);
        var eventData = await SeedPublishedEventAsync(factory, organizerId);

        using var response = await guestClient.PostAsJsonAsync(
            $"/api/events/{eventData.EventId}/orders",
            new PlaceOrderRequest(
                "Realtime Buyer",
                "realtime-buyer@example.com",
                [new PlaceOrderLineRequest(eventData.TicketTypeId, 2)]));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        notifier.EventIds.Should().Contain(eventData.EventId);

        await using var scope = factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var ticketType = await databaseContext.TicketTypes.SingleAsync(
            ticketType => ticketType.Id == eventData.TicketTypeId);
        ticketType.Sold.Should().Be(2);
        ticketType.Reserved.Should().Be(0);
    }

    [Fact]
    public async Task FailedOrder_DoesNotNotifySalesInventory()
    {
        var notifier = new RecordingRealtimeSalesInventoryNotifier();
        await using var factory = fixture.CreateFactory(services =>
        {
            services.RemoveAll<IRealtimeSalesInventoryNotifier>();
            services.AddSingleton<IRealtimeSalesInventoryNotifier>(notifier);
        });
        var organizerClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var guestClient = factory.CreateClient();
        var organizerId = await RegisterOrganizerAsync(organizerClient);
        var eventData = await SeedPublishedEventAsync(factory, organizerId);

        using var response = await guestClient.PostAsJsonAsync(
            $"/api/events/{eventData.EventId}/orders",
            new PlaceOrderRequest(
                "Oversized Buyer",
                "oversized-buyer@example.com",
                [new PlaceOrderLineRequest(eventData.TicketTypeId, 99)]));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        notifier.EventIds.Should().NotContain(eventData.EventId);
    }

    [Fact]
    public async Task EventSalesInventoryHub_AuthorizedOwnerReceivesOnlyJoinedEventUpdates()
    {
        await using var factory = fixture.CreateFactory();
        var ownerSession = await RegisterOrganizerWithSessionAsync(factory, "hub-owner");
        var otherOwnerSession = await RegisterOrganizerWithSessionAsync(factory, "hub-other-owner");
        var targetEvent = await SeedPublishedEventAsync(factory, ownerSession.UserId);
        var otherEvent = await SeedPublishedEventAsync(factory, otherOwnerSession.UserId);
        var targetUpdate = new TaskCompletionSource<EventSalesInventoryMessage>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var otherUpdate = new TaskCompletionSource<EventSalesInventoryMessage>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        await using var targetConnection = CreateHubConnection(factory, ownerSession.Cookie);
        await using var otherConnection = CreateHubConnection(factory, otherOwnerSession.Cookie);
        targetConnection.On<EventSalesInventoryMessage>(
            "eventSalesInventoryUpdated",
            message => targetUpdate.TrySetResult(message));
        otherConnection.On<EventSalesInventoryMessage>(
            "eventSalesInventoryUpdated",
            message => otherUpdate.TrySetResult(message));

        await targetConnection.StartAsync();
        await otherConnection.StartAsync();
        await targetConnection.InvokeAsync("JoinEventSalesInventory", targetEvent.EventId);
        await otherConnection.InvokeAsync("JoinEventSalesInventory", otherEvent.EventId);

        using var guestClient = factory.CreateClient();
        using var response = await guestClient.PostAsJsonAsync(
            $"/api/events/{targetEvent.EventId}/orders",
            new PlaceOrderRequest(
                "Hub Buyer",
                "hub-buyer@example.com",
                [new PlaceOrderLineRequest(targetEvent.TicketTypeId, 2)]));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var message = await targetUpdate.Task.WaitAsync(TimeSpan.FromSeconds(10));
        message.EventId.Should().Be(targetEvent.EventId);
        message.TicketTypes.Should().ContainSingle().Which.RemainingCount.Should().Be(3);

        var unexpectedOtherUpdate = await Task.WhenAny(
            otherUpdate.Task,
            Task.Delay(TimeSpan.FromMilliseconds(500)));
        unexpectedOtherUpdate.Should().NotBe(otherUpdate.Task);
    }

    [Fact]
    public async Task EventSalesInventoryHub_UserWithoutReportingPermission_CannotJoin()
    {
        await using var factory = fixture.CreateFactory();
        var ownerSession = await RegisterOrganizerWithSessionAsync(factory, "hub-deny-owner");
        var callerSession = await RegisterOrganizerWithSessionAsync(factory, "hub-deny-caller");
        var eventData = await SeedPublishedEventAsync(factory, ownerSession.UserId);

        await using var connection = CreateHubConnection(factory, callerSession.Cookie);
        await connection.StartAsync();

        var act = () => connection.InvokeAsync("JoinEventSalesInventory", eventData.EventId);

        await act.Should().ThrowAsync<HubException>();
    }

    [Fact]
    public async Task EventSalesInventoryHub_DisallowedWebSocketOrigin_IsRejected()
    {
        await using var factory = fixture.CreateFactory();
        var webSocketClient = factory.Server.CreateWebSocketClient();
        webSocketClient.ConfigureRequest = request =>
            request.Headers["Origin"] = "https://evil.example";

        var act = () => webSocketClient.ConnectAsync(
            new Uri("ws://localhost/hubs/events"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*status code: 403*");
    }

    private static async Task<Guid> RegisterOrganizerAsync(HttpClient client)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        using var response = await client.PostAsJsonAsync(
            "/api/users",
            new RegisterUserRequest(
                $"Realtime Organizer {suffix}",
                $"realtime-organizer-{suffix}@example.com",
                "SecurePass1!"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<UserRegistrationResponse>();
        body.Should().NotBeNull();
        return body!.UserId;
    }

    private static async Task<SessionData> RegisterOrganizerWithSessionAsync(
        IntegrationTestWebApplicationFactory factory,
        string prefix)
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
        });
        var suffix = Guid.NewGuid().ToString("N")[..8];
        using var response = await client.PostAsJsonAsync(
            "/api/users",
            new RegisterUserRequest(
                $"Realtime {prefix} {suffix}",
                $"{prefix}-{suffix}@example.com",
                "SecurePass1!"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<UserRegistrationResponse>();
        body.Should().NotBeNull();

        var cookie = response.Headers.GetValues("Set-Cookie")
            .Single(header => header.StartsWith("EventHub.Session=", StringComparison.Ordinal));

        return new SessionData(body!.UserId, cookie.Split(';')[0]);
    }

    private static HubConnection CreateHubConnection(
        IntegrationTestWebApplicationFactory factory,
        string sessionCookie)
    {
        return new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/events", options =>
            {
                options.Transports = HttpTransportType.LongPolling;
                options.HttpMessageHandlerFactory = _ => new SessionCookieHandler(sessionCookie)
                {
                    InnerHandler = factory.Server.CreateHandler(),
                };
            })
            .Build();
    }

    private static async Task<EventData> SeedPublishedEventAsync(
        IntegrationTestWebApplicationFactory factory,
        Guid organizerId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var eventRecord = new EventRecord
        {
            Title = $"Realtime Sales {suffix}",
            OrganizerId = organizerId,
            ScheduleStartsAt = Now.AddDays(7),
            ScheduleEndsAt = Now.AddDays(7).AddHours(2),
            ScheduleTimeZoneId = "UTC",
            LocationPhysicalAddress = "12 Live Lane",
            LocationIsOnline = false,
            Status = EventStatus.Published,
            Slug = $"realtime-sales-{suffix}",
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
            Capacity = 5,
            Sold = 0,
            Reserved = 0,
            CreatedAt = Now,
            UpdatedAt = Now,
        };
        databaseContext.TicketTypes.Add(ticketType);
        await databaseContext.SaveChangesAsync();

        return new EventData(eventRecord.Id, ticketType.Id);
    }

    private sealed record EventData(int EventId, int TicketTypeId);

    private sealed record SessionData(Guid UserId, string Cookie);

    private sealed record EventSalesInventoryMessage(
        int EventId,
        string EventTitle,
        decimal TotalRevenueAmount,
        string TotalRevenueCurrency,
        int IssuedCount,
        IReadOnlyList<TicketTypeSalesInventoryMessage> TicketTypes,
        DateTimeOffset OccurredAt);

    private sealed record TicketTypeSalesInventoryMessage(
        int TicketTypeId,
        string TicketTypeName,
        int Capacity,
        int SoldCount,
        int ReservedCount,
        int RemainingCount,
        decimal RevenueAmount,
        string RevenueCurrency);

    private sealed class SessionCookieHandler(string sessionCookie) : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            request.Headers.TryAddWithoutValidation("Cookie", sessionCookie);
            return base.SendAsync(request, cancellationToken);
        }
    }

    private sealed class RecordingRealtimeSalesInventoryNotifier : IRealtimeSalesInventoryNotifier
    {
        public List<int> EventIds { get; } = [];

        public Task NotifySalesInventoryChangedAsync(
            EventId eventId,
            CancellationToken cancellationToken = default)
        {
            EventIds.Add(eventId.Value);
            return Task.CompletedTask;
        }
    }
}
