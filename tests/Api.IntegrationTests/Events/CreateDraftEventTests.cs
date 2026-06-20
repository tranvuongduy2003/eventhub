using System.Net;
using System.Net.Http.Json;
using EventHub.Api.IntegrationTests.Integration;
using EventHub.Contracts.Events;
using EventHub.Contracts.Users;
using EventHub.Testing.Common.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EventHub.Api.IntegrationTests.Events;

[Collection(IntegrationTestCollection.Name)]
public sealed class CreateDraftEventTests(IntegrationTestFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient(
        new WebApplicationFactoryClientOptions { HandleCookies = true });

    [Fact]
    public async Task CreateDraftEvent_WithValidInput_Returns201()
    {
        await RegisterOrganizerAsync();

        var request = CreateDraftEventTestHelpers.ValidRequest();

        using var response = await _client.PostAsJsonAsync("/api/events", request);

        var draftEvent = await CreateDraftEventTestHelpers.AssertCreatedAsync(response);
        draftEvent.Status.Should().Be("Draft");
    }

    [Fact]
    public async Task CreateDraftEvent_MissingTitle_Returns422()
    {
        await RegisterOrganizerAsync();

        var request = new CreateDraftEventRequest(
            "   ",
            new DateTimeOffset(2026, 7, 15, 14, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 7, 15, 16, 0, 0, TimeSpan.Zero),
            "UTC",
            "123 Conference Ave",
            false);

        using var response = await _client.PostAsJsonAsync("/api/events", request);

        await CreateDraftEventTestHelpers.AssertValidationFailedAsync(response);
    }

    [Fact]
    public async Task CreateDraftEvent_EndBeforeStart_Returns422()
    {
        await RegisterOrganizerAsync();

        var request = new CreateDraftEventRequest(
            "Tech Conference",
            new DateTimeOffset(2026, 7, 15, 16, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 7, 15, 14, 0, 0, TimeSpan.Zero),
            "UTC",
            "123 Conference Ave",
            false);

        using var response = await _client.PostAsJsonAsync("/api/events", request);

        await CreateDraftEventTestHelpers.AssertValidationFailedAsync(response);
    }

    [Fact]
    public async Task CreateDraftEvent_NoAuth_Returns401()
    {
        var request = CreateDraftEventTestHelpers.ValidRequest();

        using var response = await _client.PostAsJsonAsync("/api/events", request);

        await CreateDraftEventTestHelpers.AssertUnauthorizedAsync(response);
    }

    [Fact]
    public async Task CreateDraftEvent_MissingLocation_Returns422()
    {
        await RegisterOrganizerAsync();

        var request = new CreateDraftEventRequest(
            "Tech Conference",
            new DateTimeOffset(2026, 7, 15, 14, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 7, 15, 16, 0, 0, TimeSpan.Zero),
            "UTC",
            null,
            false);

        using var response = await _client.PostAsJsonAsync("/api/events", request);

        await CreateDraftEventTestHelpers.AssertValidationFailedAsync(response);
    }

    [Fact]
    public async Task CreateDraftEvent_OnlineEvent_Returns201()
    {
        await RegisterOrganizerAsync();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var request = new CreateDraftEventRequest(
            $"Online Event {suffix}",
            new DateTimeOffset(2026, 7, 15, 14, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 7, 15, 16, 0, 0, TimeSpan.Zero),
            "UTC",
            null,
            true);

        using var response = await _client.PostAsJsonAsync("/api/events", request);

        var draftEvent = await CreateDraftEventTestHelpers.AssertCreatedAsync(response);
        draftEvent.Status.Should().Be("Draft");
    }

    private async Task RegisterOrganizerAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var request = new RegisterUserRequest(
            $"Organizer {suffix}",
            $"organizer_{suffix}@example.com",
            "SecurePass1!");

        using var response = await _client.PostAsJsonAsync("/api/users", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
