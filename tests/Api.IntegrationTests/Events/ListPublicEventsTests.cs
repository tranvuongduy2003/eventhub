using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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
public sealed class ListPublicEventsTests(IntegrationTestFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient(
        new WebApplicationFactoryClientOptions { HandleCookies = true });

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task ListPublicEvents_PublishedFutureEvents_Returns200WithItems()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId, startsAt: DateTimeOffset.UtcNow.AddDays(7));
        await SeedTicketTypeAsync(eventId, "General", 50m, 100);
        await PublishEventAsync(eventId);

        using var response = await _client.GetAsync("/api/events");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PublicEventListingResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
        result.Items.Should().Contain(e => e.Slug != null);
        result.TotalCount.Should().BeGreaterThan(0);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(24);
    }

    [Fact]
    public async Task ListPublicEvents_DraftEvent_Excluded()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId, startsAt: DateTimeOffset.UtcNow.AddDays(7), title: $"DraftExcluded_{Guid.NewGuid():N}");
        // Do NOT publish — stays Draft

        // Get the event's slug by publishing temporarily then checking listing would be complex.
        // Instead, verify that no item has our draft event's title.
        using var response = await _client.GetAsync("/api/events");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PublicEventListingResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.Items.Should().NotContain(e => e.Title.StartsWith("DraftExcluded_"));
    }

    [Fact]
    public async Task ListPublicEvents_PastStartAt_Excluded()
    {
        var userId = await RegisterOrganizerAsync();
        var title = $"PastEvent_{Guid.NewGuid():N}";
        var eventId = await SeedDraftEventAsync(userId, startsAt: DateTimeOffset.UtcNow.AddDays(-1), title: title);
        await SeedTicketTypeAsync(eventId, "General", 50m, 100);
        var slug = await PublishEventAsync(eventId);

        using var response = await _client.GetAsync("/api/events");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PublicEventListingResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.Items.Should().NotContain(e => e.Slug == slug);
    }

    [Fact]
    public async Task ListPublicEvents_CancelledEvent_Excluded()
    {
        var userId = await RegisterOrganizerAsync();
        var title = $"CancelledEvent_{Guid.NewGuid():N}";
        var eventId = await SeedDraftEventAsync(userId, startsAt: DateTimeOffset.UtcNow.AddDays(7), title: title);
        await SeedTicketTypeAsync(eventId, "General", 50m, 100);
        var slug = await PublishEventAsync(eventId);

        // Cancel via API
        using var cancelResponse = await _client.PostAsync($"/api/events/{eventId}/cancel", null);
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var response = await _client.GetAsync("/api/events");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PublicEventListingResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.Items.Should().NotContain(e => e.Slug == slug);
    }

    [Fact]
    public async Task ListPublicEvents_ClosedEvent_Excluded()
    {
        var userId = await RegisterOrganizerAsync();
        var title = $"ClosedEvent_{Guid.NewGuid():N}";
        var eventId = await SeedDraftEventAsync(userId, startsAt: DateTimeOffset.UtcNow.AddDays(7), title: title);
        await SeedTicketTypeAsync(eventId, "General", 50m, 100);
        var slug = await PublishEventAsync(eventId);

        // Close via API
        using var closeResponse = await _client.PostAsync($"/api/events/{eventId}/close", null);
        closeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var response = await _client.GetAsync("/api/events");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PublicEventListingResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.Items.Should().NotContain(e => e.Slug == slug);
    }

    [Fact]
    public async Task ListPublicEvents_ResponseStructure_ContainsExpectedFields()
    {
        using var response = await _client.GetAsync("/api/events");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PublicEventListingResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
        result.PageSize.Should().Be(24);
        result.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task ListPublicEvents_AnonymousAccess_Returns200()
    {
        using var unauthenticatedClient = fixture.Factory.CreateClient();

        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId, startsAt: DateTimeOffset.UtcNow.AddDays(7));
        await SeedTicketTypeAsync(eventId, "General", 50m, 100);
        await PublishEventAsync(eventId);

        using var response = await unauthenticatedClient.GetAsync("/api/events");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PublicEventListingResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ListPublicEvents_SortedByStartsAtAscending()
    {
        var userId = await RegisterOrganizerAsync();

        var eventId1 = await SeedDraftEventAsync(userId, startsAt: DateTimeOffset.UtcNow.AddDays(10), title: "Later Event");
        await SeedTicketTypeAsync(eventId1, "General", 50m, 100);
        await PublishEventAsync(eventId1);

        var eventId2 = await SeedDraftEventAsync(userId, startsAt: DateTimeOffset.UtcNow.AddDays(3), title: "Sooner Event");
        await SeedTicketTypeAsync(eventId2, "General", 30m, 100);
        await PublishEventAsync(eventId2);

        using var response = await _client.GetAsync("/api/events");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PublicEventListingResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.Items.Count.Should().BeGreaterThanOrEqualTo(2);

        // First item should have earlier start date
        var firstStartsAt = result.Items[0].StartsAt;
        var secondStartsAt = result.Items[1].StartsAt;
        firstStartsAt.Should().NotBeNull();
        secondStartsAt.Should().NotBeNull();
        (firstStartsAt <= secondStartsAt).Should().BeTrue();
    }

    [Fact]
    public async Task ListPublicEvents_ReturnsLowestPrice()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId, startsAt: DateTimeOffset.UtcNow.AddDays(7));
        await SeedTicketTypeAsync(eventId, "VIP", 150m, 50);
        await SeedTicketTypeAsync(eventId, "General", 50m, 100);
        await PublishEventAsync(eventId);

        using var response = await _client.GetAsync("/api/events");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PublicEventListingResponse>(JsonOptions);
        result.Should().NotBeNull();

        var item = result!.Items.First(e => e.Slug != null);
        item.LowestPriceAmount.Should().Be(50m);
        item.LowestPriceCurrency.Should().Be("VND");
        item.IsSoldOut.Should().BeFalse();
    }

    [Fact]
    public async Task ListPublicEvents_AllSoldOut_ReturnsIsSoldOutTrue()
    {
        var userId = await RegisterOrganizerAsync();
        var title = $"SoldOut_{Guid.NewGuid():N}";
        var eventId = await SeedDraftEventAsync(userId, startsAt: DateTimeOffset.UtcNow.AddDays(7), title: title);
        await SeedTicketTypeAsync(eventId, "General", 50m, 100, sold: 100, reserved: 0);
        var slug = await PublishEventAsync(eventId);

        using var response = await _client.GetAsync("/api/events");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PublicEventListingResponse>(JsonOptions);
        result.Should().NotBeNull();

        var item = result!.Items.First(e => e.Slug == slug);
        item.IsSoldOut.Should().BeTrue();
        item.LowestPriceAmount.Should().BeNull();
    }

    [Fact]
    public async Task ListPublicEvents_Pagination_RespectsPageSize()
    {
        var userId = await RegisterOrganizerAsync();

        // Create 3 events with unique titles
        var slugs = new List<string>();
        for (var i = 0; i < 3; i++)
        {
            var title = $"Pagination_{Guid.NewGuid():N}_{i}";
            var eventId = await SeedDraftEventAsync(userId, startsAt: DateTimeOffset.UtcNow.AddDays(7 + i), title: title);
            await SeedTicketTypeAsync(eventId, "General", 50m, 100);
            slugs.Add(await PublishEventAsync(eventId));
        }

        // Request with pageSize=2 — should return exactly 2 items
        using var response = await _client.GetAsync("/api/events?page=1&pageSize=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PublicEventListingResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
    }

    [Fact]
    public async Task ListPublicEvents_SearchByTitle_ReturnsMatching()
    {
        var userId = await RegisterOrganizerAsync();
        var title = $"UniqueSearch_{Guid.NewGuid():N}";
        var eventId = await SeedDraftEventAsync(userId, startsAt: DateTimeOffset.UtcNow.AddDays(7), title: title);
        await SeedTicketTypeAsync(eventId, "General", 50m, 100);
        var slug = await PublishEventAsync(eventId);

        // Seed another event that should NOT match
        var otherId = await SeedDraftEventAsync(userId, startsAt: DateTimeOffset.UtcNow.AddDays(8), title: $"Other_{Guid.NewGuid():N}");
        await SeedTicketTypeAsync(otherId, "General", 30m, 100);
        await PublishEventAsync(otherId);

        using var response = await _client.GetAsync($"/api/events?q={Uri.EscapeDataString(title)}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PublicEventListingResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.Items.Should().Contain(e => e.Slug == slug);
        result.Items.Should().OnlyContain(e => e.Title.Contains(title));
    }

    [Fact]
    public async Task ListPublicEvents_SearchByDescription_ReturnsMatching()
    {
        var userId = await RegisterOrganizerAsync();
        var uniquePhrase = $"DeepDive_{Guid.NewGuid():N}";
        var eventId = await SeedDraftEventAsync(
            userId,
            startsAt: DateTimeOffset.UtcNow.AddDays(7),
            title: $"Workshop {Guid.NewGuid():N}",
            description: $"Join us for a {uniquePhrase} session on advanced topics.");
        await SeedTicketTypeAsync(eventId, "General", 50m, 100);
        var slug = await PublishEventAsync(eventId);

        using var response = await _client.GetAsync($"/api/events?q={Uri.EscapeDataString(uniquePhrase)}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PublicEventListingResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.Items.Should().Contain(e => e.Slug == slug);
    }

    [Fact]
    public async Task ListPublicEvents_SearchCaseInsensitive_ReturnsMatching()
    {
        var userId = await RegisterOrganizerAsync();
        var title = $"CaseTest_{Guid.NewGuid():N}";
        var eventId = await SeedDraftEventAsync(userId, startsAt: DateTimeOffset.UtcNow.AddDays(7), title: title);
        await SeedTicketTypeAsync(eventId, "General", 50m, 100);
        var slug = await PublishEventAsync(eventId);

        using var response = await _client.GetAsync($"/api/events?q={Uri.EscapeDataString(title.ToLowerInvariant())}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PublicEventListingResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.Items.Should().Contain(e => e.Slug == slug);
    }

    [Fact]
    public async Task ListPublicEvents_SearchNoMatch_ReturnsEmpty()
    {
        var userId = await RegisterOrganizerAsync();
        var eventId = await SeedDraftEventAsync(userId, startsAt: DateTimeOffset.UtcNow.AddDays(7));
        await SeedTicketTypeAsync(eventId, "General", 50m, 100);
        await PublishEventAsync(eventId);

        using var response = await _client.GetAsync("/api/events?q=NonExistentZzzXyz123");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PublicEventListingResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ListPublicEvents_FilterByDatePreset_ThisWeek()
    {
        var userId = await RegisterOrganizerAsync();

        // Event soon (within a few days) — guaranteed to be in the future
        var thisWeekTitle = $"ThisWeek_{Guid.NewGuid():N}";
        var thisWeekId = await SeedDraftEventAsync(userId, startsAt: DateTimeOffset.UtcNow.AddDays(1), title: thisWeekTitle);
        await SeedTicketTypeAsync(thisWeekId, "General", 50m, 100);
        var thisWeekSlug = await PublishEventAsync(thisWeekId);

        // Event far future — should not match "this-week"
        var farFutureId = await SeedDraftEventAsync(userId, startsAt: DateTimeOffset.UtcNow.AddDays(60), title: $"FarFuture_{Guid.NewGuid():N}");
        await SeedTicketTypeAsync(farFutureId, "General", 30m, 100);
        await PublishEventAsync(farFutureId);

        // First verify unfiltered listing has both events
        using var unfilteredResponse = await _client.GetAsync("/api/events");
        unfilteredResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var unfilteredResult = await unfilteredResponse.Content.ReadFromJsonAsync<PublicEventListingResponse>(JsonOptions);
        unfilteredResult.Should().NotBeNull();
        unfilteredResult!.Items.Should().Contain(e => e.Slug == thisWeekSlug, "this-week event should appear in unfiltered listing");

        // Now test with date filter
        using var response = await _client.GetAsync("/api/events?date=this-week");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PublicEventListingResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.Items.Should().Contain(e => e.Slug == thisWeekSlug, "event 1 day from now should be within this-week");
        result.Items.Should().NotContain(e => e.Title.StartsWith("FarFuture_"));
    }

    [Fact]
    public async Task ListPublicEvents_FilterByDateRange_ReturnsWithinRange()
    {
        var userId = await RegisterOrganizerAsync();

        // Event within range
        var inRangeId = await SeedDraftEventAsync(userId, startsAt: new DateTimeOffset(2026, 8, 10, 14, 0, 0, TimeSpan.Zero), title: $"InRange_{Guid.NewGuid():N}");
        await SeedTicketTypeAsync(inRangeId, "General", 50m, 100);
        var inRangeSlug = await PublishEventAsync(inRangeId);

        // Event outside range
        var outRangeId = await SeedDraftEventAsync(userId, startsAt: new DateTimeOffset(2026, 12, 25, 14, 0, 0, TimeSpan.Zero), title: $"OutRange_{Guid.NewGuid():N}");
        await SeedTicketTypeAsync(outRangeId, "General", 30m, 100);
        await PublishEventAsync(outRangeId);

        using var response = await _client.GetAsync("/api/events?dateFrom=2026-08-01&dateTo=2026-08-31");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PublicEventListingResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.Items.Should().Contain(e => e.Slug == inRangeSlug);
        result.Items.Should().NotContain(e => e.Title.StartsWith("OutRange_"));
    }

    [Fact]
    public async Task ListPublicEvents_FilterByLocation_ReturnsMatching()
    {
        var userId = await RegisterOrganizerAsync();

        var hcmTitle = $"HCM_{Guid.NewGuid():N}";
        var hcmId = await SeedDraftEventAsync(
            userId,
            startsAt: DateTimeOffset.UtcNow.AddDays(7),
            title: hcmTitle,
            locationPhysicalAddress: "Ho Chi Minh City");
        await SeedTicketTypeAsync(hcmId, "General", 50m, 100);
        var hcmSlug = await PublishEventAsync(hcmId);

        var hanoiId = await SeedDraftEventAsync(
            userId,
            startsAt: DateTimeOffset.UtcNow.AddDays(8),
            title: $"Hanoi_{Guid.NewGuid():N}",
            locationPhysicalAddress: "Hanoi");
        await SeedTicketTypeAsync(hanoiId, "General", 30m, 100);
        await PublishEventAsync(hanoiId);

        using var response = await _client.GetAsync("/api/events?location=Ho+Chi+Minh+City");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PublicEventListingResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.Items.Should().Contain(e => e.Slug == hcmSlug);
        result.Items.Should().NotContain(e => e.Title.StartsWith("Hanoi_"));
    }

    [Fact]
    public async Task ListPublicEvents_FilterByOnline_ReturnsOnlyOnline()
    {
        var userId = await RegisterOrganizerAsync();

        var onlineTitle = $"Online_{Guid.NewGuid():N}";
        var onlineId = await SeedDraftEventAsync(
            userId,
            startsAt: DateTimeOffset.UtcNow.AddDays(7),
            title: onlineTitle,
            locationPhysicalAddress: null,
            locationIsOnline: true);
        await SeedTicketTypeAsync(onlineId, "General", 50m, 100);
        var onlineSlug = await PublishEventAsync(onlineId);

        var physicalId = await SeedDraftEventAsync(
            userId,
            startsAt: DateTimeOffset.UtcNow.AddDays(8),
            title: $"Physical_{Guid.NewGuid():N}",
            locationPhysicalAddress: "123 Main St",
            locationIsOnline: false);
        await SeedTicketTypeAsync(physicalId, "General", 30m, 100);
        await PublishEventAsync(physicalId);

        using var response = await _client.GetAsync("/api/events?location=Online");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PublicEventListingResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.Items.Should().Contain(e => e.Slug == onlineSlug);
        result.Items.Should().NotContain(e => e.Title.StartsWith("Physical_"));
    }

    [Fact]
    public async Task ListPublicEvents_SearchAndFilterCombined_ReturnsIntersection()
    {
        var userId = await RegisterOrganizerAsync();

        var title = $"Combined_{Guid.NewGuid():N}";
        var matchId = await SeedDraftEventAsync(
            userId,
            startsAt: DateTimeOffset.UtcNow.AddDays(2),
            title: title,
            locationPhysicalAddress: "Da Nang");
        await SeedTicketTypeAsync(matchId, "General", 50m, 100);
        var matchSlug = await PublishEventAsync(matchId);

        // Same keyword, different location
        var diffLocId = await SeedDraftEventAsync(
            userId,
            startsAt: DateTimeOffset.UtcNow.AddDays(3),
            title: title,
            locationPhysicalAddress: "Hanoi");
        await SeedTicketTypeAsync(diffLocId, "General", 30m, 100);
        await PublishEventAsync(diffLocId);

        using var response = await _client.GetAsync(
            $"/api/events?q={Uri.EscapeDataString(title)}&location=Da+Nang");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PublicEventListingResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.Items.Should().Contain(e => e.Slug == matchSlug);
        result.Items.Should().HaveCount(1);
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

    private async Task<int> SeedDraftEventAsync(
        Guid organizerId,
        DateTimeOffset? startsAt = null,
        string? title = null,
        string? description = null,
        string? locationPhysicalAddress = null,
        bool locationIsOnline = false)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var eventRecord = new EventRecord
        {
            Title = title ?? $"Tech Conference {suffix}",
            Description = description,
            OrganizerId = organizerId,
            ScheduleStartsAt = startsAt ?? new DateTimeOffset(2026, 7, 15, 14, 0, 0, TimeSpan.Zero),
            ScheduleEndsAt = (startsAt ?? new DateTimeOffset(2026, 7, 15, 14, 0, 0, TimeSpan.Zero)).AddHours(2),
            ScheduleTimeZoneId = "UTC",
            LocationPhysicalAddress = locationIsOnline ? null : locationPhysicalAddress ?? "123 Conference Ave",
            LocationIsOnline = locationIsOnline,
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
