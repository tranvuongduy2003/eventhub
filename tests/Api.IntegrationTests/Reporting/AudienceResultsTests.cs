using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EventHub.Api.Common;
using EventHub.Api.IntegrationTests.Integration;
using EventHub.Application.Abstractions.Email;
using EventHub.Application.Abstractions.Services;
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

namespace EventHub.Api.IntegrationTests.Reporting;

[Collection(IntegrationTestCollection.Name)]
public sealed class AudienceResultsTests(IntegrationTestFixture fixture)
{
    private static readonly DateTimeOffset Now = new(2026, 7, 10, 20, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AttendeeListAndResults_StaffWithReportingPermission_ReturnsAudienceData()
    {
        var emailSender = new RecordingEmailSender();
        await using var factory = CreateFactory(emailSender);
        var ownerClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var staffClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var ownerId = await RegisterUserAsync(ownerClient, "audience-owner");
        var staffId = await RegisterUserAsync(staffClient, "audience-staff");
        var data = await SeedAudienceDataAsync(factory, ownerId, staffId);

        using var attendeesResponse = await staffClient.GetAsync($"/api/events/{data.EventId}/audience/attendees");
        using var resultsResponse = await staffClient.GetAsync($"/api/events/{data.EventId}/results");

        attendeesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var attendees = await JsonDocument.ParseAsync(await attendeesResponse.Content.ReadAsStreamAsync());
        attendees.RootElement.GetProperty("attendees").GetArrayLength().Should().Be(3);
        var first = attendees.RootElement.GetProperty("attendees")[0];
        first.GetProperty("name").GetString().Should().Be("Checked Buyer");
        first.GetProperty("email").GetString().Should().Be("checked@example.com");
        first.GetProperty("ticketTypeName").GetString().Should().Be("General Admission");
        first.GetProperty("orderId").GetInt32().Should().Be(data.CheckedOrderId);
        first.GetProperty("checkedIn").GetBoolean().Should().BeTrue();

        resultsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var results = await JsonDocument.ParseAsync(await resultsResponse.Content.ReadAsStreamAsync());
        results.RootElement.GetProperty("totalRevenueAmount").GetDecimal().Should().Be(200m);
        results.RootElement.GetProperty("totalRevenueCurrency").GetString().Should().Be("VND");
        results.RootElement.GetProperty("checkedInCount").GetInt32().Should().Be(1);
        results.RootElement.GetProperty("issuedCount").GetInt32().Should().Be(3);
        results.RootElement.GetProperty("noShowCount").GetInt32().Should().Be(2);
        results.RootElement.GetProperty("checkInRate").GetDecimal().Should().BeApproximately(0.3333m, 0.0001m);
        results.RootElement.GetProperty("ticketsSoldByType").GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task AudienceEndpoints_UserWithoutReportingPermission_ReturnsForbidden()
    {
        var emailSender = new RecordingEmailSender();
        await using var factory = CreateFactory(emailSender);
        var ownerClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var callerClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var ownerId = await RegisterUserAsync(ownerClient, "audience-owner-deny");
        await RegisterUserAsync(callerClient, "audience-outsider");
        var data = await SeedAudienceDataAsync(factory, ownerId, staffUserId: null);

        using var attendeesResponse = await callerClient.GetAsync($"/api/events/{data.EventId}/audience/attendees");
        using var resultsResponse = await callerClient.GetAsync($"/api/events/{data.EventId}/results");

        attendeesResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        resultsResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var problem = await attendeesResponse.Content.ReadFromJsonAsync<ApiProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Code.Should().Be("INSUFFICIENT_PERMISSIONS");
    }

    [Fact]
    public async Task ExportMessageAndReminder_OwnerOnly()
    {
        var emailSender = new RecordingEmailSender();
        await using var factory = CreateFactory(emailSender);
        var ownerClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var staffClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var ownerId = await RegisterUserAsync(ownerClient, "audience-owner-actions");
        var staffId = await RegisterUserAsync(staffClient, "audience-staff-actions");
        var data = await SeedAudienceDataAsync(factory, ownerId, staffId);

        using var exportResponse = await ownerClient.GetAsync($"/api/events/{data.EventId}/audience/attendees.csv");
        using var staffExportResponse = await staffClient.GetAsync($"/api/events/{data.EventId}/audience/attendees.csv");
        using var messageResponse = await ownerClient.PostAsJsonAsync(
            $"/api/events/{data.EventId}/audience/messages",
            new { subject = "Reminder", body = "Doors open at 6." });
        using var staffMessageResponse = await staffClient.PostAsJsonAsync(
            $"/api/events/{data.EventId}/audience/messages",
            new { subject = "Nope", body = "Not allowed." });
        using var reminderResponse = await ownerClient.PutAsJsonAsync(
            $"/api/events/{data.EventId}/audience/reminder",
            new { enabled = true, leadTimeMinutes = 1440 });
        using var staffReminderResponse = await staffClient.PutAsJsonAsync(
            $"/api/events/{data.EventId}/audience/reminder",
            new { enabled = true, leadTimeMinutes = 1440 });
        using var lateReminderResponse = await ownerClient.PutAsJsonAsync(
            $"/api/events/{data.EventId}/audience/reminder",
            new { enabled = true, leadTimeMinutes = 20000 });

        exportResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await exportResponse.Content.ReadAsStringAsync()).Should().Contain("name,email,ticketTypeName,orderId,ticketId,checkedIn,checkedInAt");
        staffExportResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        messageResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        emailSender.Messages.Should().HaveCount(3);
        emailSender.Messages.Should().OnlyContain(message => message.Subject == "Reminder");
        staffMessageResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        reminderResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var reminder = await JsonDocument.ParseAsync(await reminderResponse.Content.ReadAsStreamAsync());
        reminder.RootElement.GetProperty("enabled").GetBoolean().Should().BeTrue();
        reminder.RootElement.GetProperty("leadTimeMinutes").GetInt32().Should().Be(1440);
        staffReminderResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        lateReminderResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task OrganizerOverview_SeparatesOwnedAndStaffEvents()
    {
        var emailSender = new RecordingEmailSender();
        await using var factory = CreateFactory(emailSender);
        var ownerClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var callerClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var ownerId = await RegisterUserAsync(ownerClient, "overview-other-owner");
        var callerId = await RegisterUserAsync(callerClient, "overview-caller");
        var owned = await SeedAudienceDataAsync(factory, callerId, staffUserId: null, title: "Owned Overview");
        var staffed = await SeedAudienceDataAsync(factory, ownerId, callerId, title: "Staff Overview");

        using var response = await callerClient.GetAsync("/api/organizer/audience/events");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var overview = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var ownedEvents = overview.RootElement.GetProperty("ownedEvents");
        var staffEvents = overview.RootElement.GetProperty("staffEvents");
        ownedEvents.EnumerateArray().Should().Contain(item => item.GetProperty("eventId").GetInt32() == owned.EventId);
        ownedEvents[0].TryGetProperty("totalRevenueAmount", out _).Should().BeTrue();
        staffEvents.EnumerateArray().Should().Contain(item => item.GetProperty("eventId").GetInt32() == staffed.EventId);
        staffEvents[0].TryGetProperty("totalRevenueAmount", out _).Should().BeFalse();
        staffEvents[0].TryGetProperty("checkedInCount", out _).Should().BeTrue();
    }

    private IntegrationTestWebApplicationFactory CreateFactory(RecordingEmailSender emailSender) =>
        fixture.CreateFactory(services =>
        {
            services.RemoveAll<IClock>();
            services.AddSingleton<IClock>(new TestClock { UtcNow = Now });
            services.RemoveAll<IHostedService>();
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(emailSender);
        });

    private static async Task<Guid> RegisterUserAsync(HttpClient client, string suffix)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/users",
            new RegisterUserRequest(
                $"User {suffix}",
                $"{suffix}_{Guid.NewGuid():N}@example.com",
                "SecurePass1!"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<UserRegistrationResponse>();
        body.Should().NotBeNull();
        return body!.UserId;
    }

    private static async Task<AudienceData> SeedAudienceDataAsync(
        IntegrationTestWebApplicationFactory factory,
        Guid ownerId,
        Guid? staffUserId,
        string title = "Audience Event")
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var eventRecord = new EventRecord
        {
            Title = $"{title} {suffix}",
            OrganizerId = ownerId,
            ScheduleStartsAt = Now.AddDays(7),
            ScheduleEndsAt = Now.AddDays(7).AddHours(2),
            ScheduleTimeZoneId = "UTC",
            LocationPhysicalAddress = "42 Result St",
            LocationIsOnline = false,
            Status = EventStatus.Published,
            Slug = $"audience-{suffix}",
            CreatedAt = Now,
            UpdatedAt = Now,
        };
        databaseContext.Events.Add(eventRecord);
        await databaseContext.SaveChangesAsync();

        var general = new TicketTypeRecord
        {
            EventId = eventRecord.Id,
            Name = "General Admission",
            PriceAmount = 50m,
            PriceCurrency = "VND",
            Capacity = 10,
            Sold = 2,
            Reserved = 0,
            CreatedAt = Now,
            UpdatedAt = Now,
        };
        var vip = new TicketTypeRecord
        {
            EventId = eventRecord.Id,
            Name = "VIP",
            PriceAmount = 100m,
            PriceCurrency = "VND",
            Capacity = 5,
            Sold = 1,
            Reserved = 0,
            CreatedAt = Now,
            UpdatedAt = Now,
        };
        databaseContext.TicketTypes.AddRange(general, vip);
        await databaseContext.SaveChangesAsync();

        if (staffUserId is not null)
        {
            databaseContext.EventUserRoles.Add(new EventUserRoleRecord
            {
                EventId = eventRecord.Id,
                UserId = staffUserId.Value,
                Role = EventRole.Staff,
                CreatedAt = Now,
            });
        }

        var checkedOrder = CreateOrder(eventRecord.Id, "Checked Buyer", "checked@example.com", 50m, general.Id, 1);
        var multiOrder = CreateOrder(eventRecord.Id, "Multi Buyer", "multi@example.com", 150m, general.Id, 1);
        multiOrder.Lines.Add(new OrderLineRecord
        {
            TicketTypeId = vip.Id,
            Quantity = 1,
            UnitPriceAmount = 100m,
            UnitPriceCurrency = "VND",
            LineTotalAmount = 100m,
            LineTotalCurrency = "VND",
        });
        databaseContext.Orders.AddRange(checkedOrder, multiOrder);
        await databaseContext.SaveChangesAsync();

        databaseContext.Tickets.AddRange(
            CreateTicket(eventRecord.Id, checkedOrder.Id, general.Id, "Checked Buyer", "checked@example.com", true),
            CreateTicket(eventRecord.Id, multiOrder.Id, general.Id, "Multi Buyer", "multi@example.com", false),
            CreateTicket(eventRecord.Id, multiOrder.Id, vip.Id, "Multi Buyer", "multi@example.com", false));
        await databaseContext.SaveChangesAsync();

        return new AudienceData(eventRecord.Id, checkedOrder.Id);
    }

    private static OrderRecord CreateOrder(
        int eventId,
        string contactName,
        string contactEmail,
        decimal totalAmount,
        int ticketTypeId,
        int quantity) =>
        new()
        {
            EventId = eventId,
            ContactName = contactName,
            ContactEmail = contactEmail,
            Status = OrderStatus.Confirmed.ToString(),
            TotalAmount = totalAmount,
            TotalCurrency = "VND",
            PlacedAt = Now.AddHours(-2),
            ConfirmedAt = Now.AddHours(-2),
            RowVersion = 1,
            Lines =
            [
                new OrderLineRecord
                {
                    TicketTypeId = ticketTypeId,
                    Quantity = quantity,
                    UnitPriceAmount = totalAmount / quantity,
                    UnitPriceCurrency = "VND",
                    LineTotalAmount = totalAmount,
                    LineTotalCurrency = "VND",
                },
            ],
        };

    private static TicketRecord CreateTicket(
        int eventId,
        int orderId,
        int ticketTypeId,
        string holderName,
        string holderEmail,
        bool checkedIn) =>
        new()
        {
            EventId = eventId,
            OrderId = orderId,
            TicketTypeId = ticketTypeId,
            Code = $"tk_{Guid.NewGuid():N}",
            HolderName = holderName,
            HolderEmail = holderEmail,
            Status = checkedIn ? "CheckedIn" : "Valid",
            IssuedAt = Now.AddHours(-1),
            CheckedInAt = checkedIn ? Now : null,
            RowVersion = 1,
        };

    private sealed record AudienceData(int EventId, int CheckedOrderId);

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
