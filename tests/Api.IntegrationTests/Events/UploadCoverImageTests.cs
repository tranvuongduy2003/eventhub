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
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.Api.IntegrationTests.Events;

[Collection(IntegrationTestCollection.Name)]
public sealed class UploadCoverImageTests(IntegrationTestFixture fixture)
{
    private sealed record EventSetup(Guid UserId, int EventId);

    private readonly HttpClient _client = fixture.Factory.CreateClient(
        new WebApplicationFactoryClientOptions { HandleCookies = true });

    [Fact]
    public async Task UploadCoverImage_WithValidJpeg_Returns200()
    {
        var setup = await SetupEventWithOwnerAsync();

        using var content = CreateMultipartContent("cover.jpg", "image/jpeg");
        using var response = await _client.PutAsync($"/api/events/{setup.EventId}/cover-image", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CoverImageResponse>();
        result.Should().NotBeNull();
        result!.CoverImageUrl.Should().Contain("events/");
        result.CoverImageUrl.Should().Contain("cover/");
    }

    [Fact]
    public async Task UploadCoverImage_WithValidPng_Returns200()
    {
        var setup = await SetupEventWithOwnerAsync();

        using var content = CreateMultipartContent("cover.png", "image/png");
        using var response = await _client.PutAsync($"/api/events/{setup.EventId}/cover-image", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UploadCoverImage_WithValidWebP_Returns200()
    {
        var setup = await SetupEventWithOwnerAsync();

        using var content = CreateMultipartContent("cover.webp", "image/webp");
        using var response = await _client.PutAsync($"/api/events/{setup.EventId}/cover-image", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UploadCoverImage_WithInvalidContentType_Returns422()
    {
        var setup = await SetupEventWithOwnerAsync();

        using var content = CreateMultipartContent("document.pdf", "application/pdf");
        using var response = await _client.PutAsync($"/api/events/{setup.EventId}/cover-image", content);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UploadCoverImage_WhenNotAuthenticated_Returns401()
    {
        using var client = fixture.Factory.CreateClient();
        using var content = CreateMultipartContent("cover.jpg", "image/jpeg");
        using var response = await client.PutAsync("/api/events/1/cover-image", content);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UploadCoverImage_WhenEventNotFound_Returns404()
    {
        await RegisterOrganizerAsync();

        using var content = CreateMultipartContent("cover.jpg", "image/jpeg");
        using var response = await _client.PutAsync("/api/events/999999/cover-image", content);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UploadCoverImage_CanReplaceExistingCoverImage()
    {
        var setup = await SetupEventWithOwnerAsync();

        using var content1 = CreateMultipartContent("cover1.jpg", "image/jpeg");
        using var response1 = await _client.PutAsync($"/api/events/{setup.EventId}/cover-image", content1);
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        var result1 = await response1.Content.ReadFromJsonAsync<CoverImageResponse>();

        using var content2 = CreateMultipartContent("cover2.jpg", "image/jpeg");
        using var response2 = await _client.PutAsync($"/api/events/{setup.EventId}/cover-image", content2);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        var result2 = await response2.Content.ReadFromJsonAsync<CoverImageResponse>();

        result1!.CoverImageUrl.Should().NotBe(result2!.CoverImageUrl);
    }

    private static MultipartFormDataContent CreateMultipartContent(string fileName, string contentType)
    {
        var fileContent = new byte[1024];
        Random.Shared.NextBytes(fileContent);

        var fileStream = new MemoryStream(fileContent);
        var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

        var multipartContent = new MultipartFormDataContent();
        multipartContent.Add(streamContent, "file", fileName);

        return multipartContent;
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
        var registration = await response.Content.ReadFromJsonAsync<UserRegistrationResponse>();
        return registration!.UserId;
    }

    private async Task<EventSetup> SetupEventWithOwnerAsync()
    {
        var userId = await RegisterOrganizerAsync();

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        var eventId = Random.Shared.Next(10000, 99999);

        databaseContext.Events.Add(new EventRecord
        {
            Id = eventId,
            OrganizerId = userId,
            Title = "Test Event",
            ScheduleStartsAt = new DateTimeOffset(2026, 7, 15, 14, 0, 0, TimeSpan.Zero),
            ScheduleEndsAt = new DateTimeOffset(2026, 7, 15, 16, 0, 0, TimeSpan.Zero),
            ScheduleTimeZoneId = "UTC",
            LocationPhysicalAddress = "123 Main St",
            LocationIsOnline = false,
            Status = EventStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        });

        databaseContext.EventUserRoles.Add(new EventUserRoleRecord
        {
            EventId = eventId,
            UserId = userId,
            Role = EventRole.Owner,
            CreatedAt = DateTimeOffset.UtcNow,
        });

        await databaseContext.SaveChangesAsync();

        return new EventSetup(userId, eventId);
    }
}
