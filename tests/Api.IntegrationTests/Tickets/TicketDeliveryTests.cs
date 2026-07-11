using System.Net;
using System.Net.Http.Json;
using EventHub.Api.IntegrationTests.Integration;
using EventHub.Application.Abstractions.Email;
using EventHub.Application.Abstractions.Services;
using EventHub.Contracts.Orders;
using EventHub.Contracts.Payments;
using EventHub.Contracts.Tickets;
using EventHub.Contracts.Users;
using EventHub.Domain.Events;
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
public sealed class TicketDeliveryTests(IntegrationTestFixture fixture)
{
    private static readonly DateTimeOffset Start = new(2026, 7, 10, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task PlaceFreeOrder_ConfirmedOrder_IssuesOneTicketPerUnitAndSendsEmail()
    {
        var emailSender = new RecordingEmailSender();
        await using var factory = CreateFactory(emailSender);
        await ClearTicketDataAsync(factory);
        var guestClient = factory.CreateClient();
        var organizerClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        var data = await SeedPublishedEventAsync(factory, organizerClient, capacity: 5, priceAmount: 0m);
        var order = await PlaceOrderAsync(guestClient, data.EventId, data.TicketTypeId, quantity: 3, "jane@example.com");

        order.Status.Should().Be("confirmed");

        await using var scope = factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var tickets = await databaseContext.Tickets
            .Where(ticket => ticket.OrderId == order.OrderId)
            .ToListAsync();

        tickets.Should().HaveCount(3);
        tickets.Select(ticket => ticket.Code).Should().OnlyHaveUniqueItems();
        tickets.Should().OnlyContain(ticket =>
            ticket.EventId == data.EventId
            && ticket.TicketTypeId == data.TicketTypeId
            && ticket.HolderEmail == "jane@example.com"
            && ticket.Status == "Valid");
        emailSender.Messages.Should().ContainSingle(message =>
            message.Recipient == "jane@example.com"
            && message.HtmlBody.Contains("/tickets/orders/", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ConfirmPayment_DuplicateNotification_DoesNotDuplicateTickets()
    {
        var emailSender = new RecordingEmailSender();
        await using var factory = CreateFactory(emailSender);
        await ClearTicketDataAsync(factory);
        var guestClient = factory.CreateClient();
        var organizerClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        var data = await SeedPublishedEventAsync(factory, organizerClient, capacity: 5, priceAmount: 25m);
        var order = await PlaceOrderAsync(guestClient, data.EventId, data.TicketTypeId, quantity: 2, "jane@example.com");
        var payment = await StartPaymentAsync(guestClient, order.OrderId);

        using var firstResponse = await guestClient.PostAsJsonAsync(
            "/api/payments/provider-notifications/succeeded",
            new PaymentProviderNotificationRequest(payment.ProviderReference));
        using var secondResponse = await guestClient.PostAsJsonAsync(
            "/api/payments/provider-notifications/succeeded",
            new PaymentProviderNotificationRequest(payment.ProviderReference));

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var tickets = await databaseContext.Tickets
            .Where(ticket => ticket.OrderId == order.OrderId)
            .ToListAsync();

        tickets.Should().HaveCount(2);
        tickets.Select(ticket => ticket.Code).Should().OnlyHaveUniqueItems();
        emailSender.Messages.Should().ContainSingle();
    }

    [Fact]
    public async Task GetOrderTickets_AnonymousBuyer_ReturnsIssuedTickets()
    {
        var emailSender = new RecordingEmailSender();
        await using var factory = CreateFactory(emailSender);
        await ClearTicketDataAsync(factory);
        var guestClient = factory.CreateClient();
        var organizerClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        var data = await SeedPublishedEventAsync(factory, organizerClient, capacity: 5, priceAmount: 0m);
        var order = await PlaceOrderAsync(guestClient, data.EventId, data.TicketTypeId, quantity: 2, "jane@example.com");

        using var response = await guestClient.GetAsync($"/api/orders/{order.OrderId}/tickets");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<OrderTicketsResponse>();
        body.Should().NotBeNull();
        body!.OrderStatus.Should().Be("confirmed");
        body.Tickets.Should().HaveCount(2);
        body.Tickets.Should().OnlyContain(ticket =>
            ticket.EventTitle == data.EventTitle
            && ticket.TicketTypeName == "General Admission"
            && ticket.HolderEmail == "jane@example.com"
            && ticket.Code.StartsWith("tk_", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ResendTickets_MatchingEmail_RedeliversWithoutCreatingTickets()
    {
        var emailSender = new RecordingEmailSender();
        await using var factory = CreateFactory(emailSender);
        await ClearTicketDataAsync(factory);
        var guestClient = factory.CreateClient();
        var organizerClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        var data = await SeedPublishedEventAsync(factory, organizerClient, capacity: 5, priceAmount: 0m);
        var order = await PlaceOrderAsync(guestClient, data.EventId, data.TicketTypeId, quantity: 1, "jane@example.com");

        using var response = await guestClient.PostAsJsonAsync(
            $"/api/orders/{order.OrderId}/tickets/resend",
            new ResendTicketsRequest("jane@example.com"));

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var resend = await response.Content.ReadFromJsonAsync<ResendTicketsResponse>();
        resend.Should().NotBeNull();
        resend!.Accepted.Should().BeTrue();

        await using var scope = factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        (await databaseContext.Tickets.CountAsync(ticket => ticket.OrderId == order.OrderId)).Should().Be(1);
        emailSender.Messages.Should().HaveCount(2);
    }

    [Fact]
    public async Task TransferTicket_ValidTicket_InvalidatesOldCodeAndIssuesFreshRecipientTicket()
    {
        var emailSender = new RecordingEmailSender();
        await using var factory = CreateFactory(emailSender);
        await ClearTicketDataAsync(factory);
        var guestClient = factory.CreateClient();
        var organizerClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var staffClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        var staffId = await RegisterStaffAsync(staffClient);
        var data = await SeedPublishedEventAsync(factory, organizerClient, capacity: 5, priceAmount: 0m);
        await SeedStaffRoleAsync(factory, data.EventId, staffId);
        var order = await PlaceOrderAsync(guestClient, data.EventId, data.TicketTypeId, quantity: 1, "jane@example.com");

        await using var beforeScope = factory.Services.CreateAsyncScope();
        var beforeDatabaseContext = beforeScope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var originalTicket = await beforeDatabaseContext.Tickets.SingleAsync(ticket => ticket.OrderId == order.OrderId);
        var originalCode = originalTicket.Code;

        typeof(TransferTicketRequest).GetProperties()
            .Select(property => property.Name)
            .Should().BeEquivalentTo(["RecipientName", "RecipientEmail"]);

        using var transferResponse = await guestClient.PostAsJsonAsync(
            $"/api/orders/{order.OrderId}/tickets/{originalTicket.Id}/transfer",
            new TransferTicketRequest("Recipient Friend", "Recipient@Example.com"));

        transferResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var transfer = await transferResponse.Content.ReadFromJsonAsync<TicketResponse>();
        transfer.Should().NotBeNull();
        transfer!.HolderEmail.Should().Be("recipient@example.com");
        transfer.Code.Should().NotBe(originalCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var tickets = await databaseContext.Tickets
            .Where(ticket => ticket.OrderId == order.OrderId)
            .OrderBy(ticket => ticket.Id)
            .ToListAsync();

        tickets.Should().HaveCount(2);
        tickets[0].Status.Should().Be("Transferred");
        tickets[0].Code.Should().Be(originalCode);
        tickets[1].Status.Should().Be("Valid");
        tickets[1].Code.Should().NotBe(originalCode);
        tickets[1].HolderName.Should().Be("Recipient Friend");
        tickets[1].HolderEmail.Should().Be("recipient@example.com");
        emailSender.Messages.Should().Contain(message => message.Recipient == "recipient@example.com");

        using var oldCodeResponse = await staffClient.PostAsJsonAsync(
            $"/api/events/{data.EventId}/check-ins/scan",
            new CheckInTicketRequest(originalCode));
        using var newCodeResponse = await staffClient.PostAsJsonAsync(
            $"/api/events/{data.EventId}/check-ins/scan",
            new CheckInTicketRequest(tickets[1].Code));

        oldCodeResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var oldCodeProblem = await oldCodeResponse.Content.ReadFromJsonAsync<EventHub.Api.Common.ApiProblemDetails>();
        oldCodeProblem.Should().NotBeNull();
        oldCodeProblem!.Code.Should().Be("TICKET_NOT_VALID_FOR_CHECK_IN");

        newCodeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var checkedIn = await newCodeResponse.Content.ReadFromJsonAsync<CheckInTicketResponse>();
        checkedIn.Should().NotBeNull();
        checkedIn!.HolderEmail.Should().Be("recipient@example.com");
    }

    [Fact]
    public async Task TransferTicket_CheckedInTicket_Returns422WithoutReplacement()
    {
        var emailSender = new RecordingEmailSender();
        await using var factory = CreateFactory(emailSender);
        await ClearTicketDataAsync(factory);
        var guestClient = factory.CreateClient();
        var organizerClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var staffClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        var staffId = await RegisterStaffAsync(staffClient);
        var data = await SeedPublishedEventAsync(factory, organizerClient, capacity: 5, priceAmount: 0m);
        await SeedStaffRoleAsync(factory, data.EventId, staffId);
        var order = await PlaceOrderAsync(guestClient, data.EventId, data.TicketTypeId, quantity: 1, "jane@example.com");

        await using var beforeScope = factory.Services.CreateAsyncScope();
        var beforeDatabaseContext = beforeScope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var originalTicket = await beforeDatabaseContext.Tickets.SingleAsync(ticket => ticket.OrderId == order.OrderId);

        using var checkInResponse = await staffClient.PostAsJsonAsync(
            $"/api/events/{data.EventId}/check-ins/scan",
            new CheckInTicketRequest(originalTicket.Code));
        checkInResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var transferResponse = await guestClient.PostAsJsonAsync(
            $"/api/orders/{order.OrderId}/tickets/{originalTicket.Id}/transfer",
            new TransferTicketRequest("Recipient Friend", "recipient@example.com"));

        transferResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await transferResponse.Content.ReadFromJsonAsync<EventHub.Api.Common.ApiProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Code.Should().Be("TICKET_ALREADY_CHECKED_IN");

        await using var scope = factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        (await databaseContext.Tickets.CountAsync(ticket => ticket.OrderId == order.OrderId)).Should().Be(1);
    }

    [Fact]
    public async Task GetMyTickets_AttendeeSession_ReturnsTicketsForAccountEmail()
    {
        var emailSender = new RecordingEmailSender();
        await using var factory = CreateFactory(emailSender);
        await ClearTicketDataAsync(factory);
        var guestClient = factory.CreateClient();
        var organizerClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var attendeeClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        var data = await SeedPublishedEventAsync(factory, organizerClient, capacity: 5, priceAmount: 0m);
        await PlaceOrderAsync(guestClient, data.EventId, data.TicketTypeId, quantity: 2, "wallet@example.com");
        await RegisterAttendeeAsync(attendeeClient, "wallet@example.com");

        using var response = await attendeeClient.GetAsync("/api/me/tickets");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MyTicketsResponse>();
        body.Should().NotBeNull();
        body!.Tickets.Should().HaveCount(2);
        body.Tickets.Should().OnlyContain(ticket => ticket.HolderEmail == "wallet@example.com");
    }

    private IntegrationTestWebApplicationFactory CreateFactory(RecordingEmailSender emailSender) =>
        fixture.CreateFactory(services =>
        {
            services.RemoveAll<IClock>();
            services.AddSingleton<IClock>(new TestClock { UtcNow = Start });
            services.RemoveAll<IHostedService>();
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(emailSender);
        });

    private static async Task ClearTicketDataAsync(IntegrationTestWebApplicationFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        await databaseContext.Tickets.ExecuteDeleteAsync();
        await databaseContext.Payments.ExecuteDeleteAsync();
        await databaseContext.Reservations.ExecuteDeleteAsync();
        await databaseContext.OrderLines.ExecuteDeleteAsync();
        await databaseContext.Orders.ExecuteDeleteAsync();
    }

    private static async Task<PlaceOrderResponse> PlaceOrderAsync(
        HttpClient client,
        int eventId,
        int ticketTypeId,
        int quantity,
        string email)
    {
        using var response = await client.PostAsJsonAsync(
            $"/api/events/{eventId}/orders",
            new PlaceOrderRequest(
                "Jane Attendee",
                email,
                [new PlaceOrderLineRequest(ticketTypeId, quantity)]));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<PlaceOrderResponse>();
        body.Should().NotBeNull();
        return body!;
    }

    private static async Task<StartPaymentResponse> StartPaymentAsync(HttpClient client, int orderId)
    {
        using var response = await client.PostAsJsonAsync(
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
        var eventTitle = $"Ticket Event {suffix}";
        var eventRecord = new EventRecord
        {
            Title = eventTitle,
            OrganizerId = organizerId,
            ScheduleStartsAt = new DateTimeOffset(2026, 7, 15, 14, 0, 0, TimeSpan.Zero),
            ScheduleEndsAt = new DateTimeOffset(2026, 7, 15, 16, 0, 0, TimeSpan.Zero),
            ScheduleTimeZoneId = "UTC",
            LocationPhysicalAddress = "123 Ticket Ave",
            LocationIsOnline = false,
            Status = EventStatus.Published,
            Slug = $"tickets-{suffix}",
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

        return new EventData(eventRecord.Id, ticketTypeRecord.Id, eventTitle);
    }

    private static async Task<Guid> RegisterOrganizerAsync(
        IntegrationTestWebApplicationFactory factory,
        HttpClient organizerClient)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"ticket_org_{suffix}@example.com";

        using var response = await organizerClient.PostAsJsonAsync(
            "/api/users",
            new RegisterUserRequest($"Organizer {suffix}", email, "SecurePass1!"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        await using var scope = factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var user = await databaseContext.Users.SingleAsync(user => user.Email == email);
        return user.Id;
    }

    private static async Task RegisterAttendeeAsync(HttpClient attendeeClient, string email)
    {
        using var response = await attendeeClient.PostAsJsonAsync(
            "/api/attendees",
            new RegisterUserRequest("Wallet Attendee", email, "SecurePass1!"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private static async Task<Guid> RegisterStaffAsync(HttpClient staffClient)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        using var response = await staffClient.PostAsJsonAsync(
            "/api/users",
            new RegisterUserRequest($"Staff {suffix}", $"staff_{suffix}@example.com", "SecurePass1!"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<UserRegistrationResponse>();
        body.Should().NotBeNull();
        return body!.UserId;
    }

    private static async Task SeedStaffRoleAsync(
        IntegrationTestWebApplicationFactory factory,
        int eventId,
        Guid staffId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        databaseContext.EventUserRoles.Add(new EventUserRoleRecord
        {
            EventId = eventId,
            UserId = staffId,
            Role = EventRole.Staff,
            CreatedAt = Start,
        });
        await databaseContext.SaveChangesAsync();
    }

    private sealed record EventData(int EventId, int TicketTypeId, string EventTitle);

    private sealed class RecordingEmailSender : IEmailSender
    {
        public List<EmailMessage> Messages { get; } = [];

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }
}
