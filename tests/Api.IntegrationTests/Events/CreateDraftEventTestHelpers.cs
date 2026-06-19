using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EventHub.Api.Common;
using EventHub.Contracts.Events;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace EventHub.Api.IntegrationTests.Events;

internal static class CreateDraftEventTestHelpers
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static CreateDraftEventRequest ValidRequest(string? suffix = null)
    {
        suffix ??= Guid.NewGuid().ToString("N")[..8];
        return new CreateDraftEventRequest(
            $"Tech Conference {suffix}",
            new DateTimeOffset(2026, 7, 15, 14, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 7, 15, 16, 0, 0, TimeSpan.Zero),
            "UTC",
            "123 Conference Ave",
            false);
    }

    public static async Task<DraftEventResponse> AssertCreatedAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var draftEvent = await response.Content.ReadFromJsonAsync<DraftEventResponse>(JsonOptions);
        draftEvent.Should().NotBeNull();
        draftEvent!.EventId.Should().BeGreaterThan(0);
        draftEvent.Status.Should().Be("Draft");

        return draftEvent;
    }

    public static async Task<ApiProblemDetails> AssertValidationFailedAsync(
        HttpResponseMessage response,
        string expectedCode = "VALIDATION_FAILED")
    {
        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(
            HttpStatusCode.UnprocessableEntity,
            $"expected 422 but got {(int)response.StatusCode} with body: {responseBody}");

        var problem = JsonSerializer.Deserialize<ApiProblemDetails>(responseBody, JsonOptions);
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status422UnprocessableEntity);
        problem.Code.Should().Be(expectedCode);

        return problem;
    }

    public static async Task AssertUnauthorizedAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    public static StringContent JsonContent(string json) =>
        new(json, System.Text.Encoding.UTF8, "application/json");
}
