using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using EventHub.Api.IntegrationTests.Integration;
using EventHub.Contracts.Events;
using EventHub.Contracts.Users;
using EventHub.Domain.Events;
using EventHub.Infrastructure.Persistence;
using EventHub.Infrastructure.Persistence.Entities;
using EventHub.Testing.Common.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.Api.IntegrationTests.Events;

[Collection(IntegrationTestCollection.Name)]
public sealed partial class OpenGraphTests(IntegrationTestFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient(
        new WebApplicationFactoryClientOptions { HandleCookies = true });

    private readonly HttpClient _htmlClient = CreateHtmlClient(fixture.Factory);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task PublishedEvent_WithCover_ReturnsHtmlWithOgTags()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId);
        await SeedTicketTypeAsync(eventId, "General Admission", 50m, 100);
        var slug = await PublishEventAsync(eventId);

        using var response = await _htmlClient.GetAsync($"/api/events/{slug}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("og:title");
        html.Should().Contain("og:description");
        html.Should().Contain("og:type");
        html.Should().Contain("og:url");
        html.Should().Contain("og:site_name");
        html.Should().Contain("og:image");
        html.Should().Contain("og:image:alt");
        html.Should().Contain("twitter:card");
        html.Should().Contain("twitter:title");
        html.Should().Contain("twitter:description");
        html.Should().Contain("twitter:image");
    }

    [Fact]
    public async Task PublishedEvent_WithoutCover_ReturnsHtmlWithoutOgImage()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId, withCover: false);
        await SeedTicketTypeAsync(eventId, "General Admission", 50m, 100);
        var slug = await PublishEventAsync(eventId);

        using var response = await _htmlClient.GetAsync($"/api/events/{slug}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("og:title");
        html.Should().Contain("og:description");
        html.Should().NotContain("og:image");
        html.Should().NotContain("twitter:image");
    }

    [Fact]
    public async Task PublishedEvent_SetsTwitterCardToSummaryLargeImage_WhenCoverExists()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId);
        await SeedTicketTypeAsync(eventId, "General Admission", 50m, 100);
        var slug = await PublishEventAsync(eventId);

        using var response = await _htmlClient.GetAsync($"/api/events/{slug}");

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("twitter:card");
        html.Should().Contain("summary_large_image");
    }

    [Fact]
    public async Task PublishedEvent_SetsTwitterCardToSummary_WhenNoCover()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId, withCover: false);
        await SeedTicketTypeAsync(eventId, "General Admission", 50m, 100);
        var slug = await PublishEventAsync(eventId);

        using var response = await _htmlClient.GetAsync($"/api/events/{slug}");

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("twitter:card");
        html.Should().Contain("\"summary\"");
        html.Should().NotContain("summary_large_image");
    }

    [Fact]
    public async Task DraftEvent_Returns404()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId);

        // Draft events don't have a slug, use a fake one
        using var response = await _htmlClient.GetAsync("/api/events/draft-event-slug");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task NonExistentSlug_Returns404()
    {
        using var response = await _htmlClient.GetAsync("/api/events/non-existent-slug-xyz");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PublishedEvent_OgTypeIsEvent()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId);
        await SeedTicketTypeAsync(eventId, "General Admission", 50m, 100);
        var slug = await PublishEventAsync(eventId);

        using var response = await _htmlClient.GetAsync($"/api/events/{slug}");

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("<meta property=\"og:type\" content=\"event\">");
    }

    [Fact]
    public async Task PublishedEvent_OgTitleMatchesEventTitle()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId, title: "My Special Conference 2026");
        await SeedTicketTypeAsync(eventId, "General Admission", 50m, 100);
        var slug = await PublishEventAsync(eventId);

        using var response = await _htmlClient.GetAsync($"/api/events/{slug}");

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("<meta property=\"og:title\" content=\"My Special Conference 2026\">");
    }

    [Fact]
    public async Task PublishedEvent_OgDescriptionContainsDateAndLocation()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId);
        await SeedTicketTypeAsync(eventId, "General Admission", 50m, 100);
        var slug = await PublishEventAsync(eventId);

        using var response = await _htmlClient.GetAsync($"/api/events/{slug}");

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("og:description");
        html.Should().Contain("123 Conference Ave");
    }

    [Fact]
    public async Task JsonRequest_ReturnsNormalJsonResponse()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId);
        await SeedTicketTypeAsync(eventId, "General Admission", 50m, 100);
        var slug = await PublishEventAsync(eventId);

        // Default client sends Accept: application/json
        using var response = await _client.GetAsync($"/api/events/{slug}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PublicEventResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.Slug.Should().Be(slug);
    }

    private static HttpClient CreateHtmlClient(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Accept.ParseAdd("text/html");
        return client;
    }

    private async Task<Guid> RegisterOrganizerAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var request = new RegisterUserRequest(
            $"Organizer_{suffix}",
            $"organizer_{suffix}@example.com",
            "SecurePass1!");

        using var response = await _client.PostAsJsonAsync("/api/users", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var user = databaseContext.Users.OrderByDescending(u => u.CreatedAt).First();
        return user.Id;
    }

    private async Task<int> SeedDraftEventAsync(Guid organizerId, bool withCover = true, string? title = null)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var eventRecord = new EventRecord
        {
            Title = title ?? $"Tech Conference {suffix}",
            OrganizerId = organizerId,
            ScheduleStartsAt = new DateTimeOffset(2026, 7, 15, 14, 0, 0, TimeSpan.Zero),
            ScheduleEndsAt = new DateTimeOffset(2026, 7, 15, 16, 0, 0, TimeSpan.Zero),
            ScheduleTimeZoneId = "UTC",
            LocationPhysicalAddress = "123 Conference Ave",
            LocationIsOnline = false,
            CoverImageKey = withCover ? $"covers/{suffix}/cover.jpg" : null,
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

    private async Task SeedTicketTypeAsync(
        int eventId,
        string name,
        decimal priceAmount,
        int capacity,
        int sold = 0,
        int reserved = 0)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        databaseContext.TicketTypes.Add(new TicketTypeRecord
        {
            EventId = eventId,
            Name = name,
            PriceAmount = priceAmount,
            PriceCurrency = "VND",
            Capacity = capacity,
            Sold = sold,
            Reserved = reserved,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        });
        await databaseContext.SaveChangesAsync();
    }

    private async Task<string> PublishEventAsync(int eventId)
    {
        using var response = await _client.PostAsync($"/api/events/{eventId}/publish", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PublishEventResponse>(JsonOptions);
        return result!.Slug;
    }
}
