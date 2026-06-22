using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EventHub.Api.Common;
using EventHub.Contracts.Events;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace EventHub.Api.IntegrationTests.Events;

internal static class PublishEventTestHelpers
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<PublishEventResponse> AssertPublishedAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var publishResult = await response.Content.ReadFromJsonAsync<PublishEventResponse>(JsonOptions);
        publishResult.Should().NotBeNull();
        publishResult!.Status.Should().Be("Published");
        publishResult.Slug.Should().NotBeNullOrWhiteSpace();

        return publishResult;
    }

    public static async Task<ApiProblemDetails> AssertNotPublishableAsync(HttpResponseMessage response)
    {
        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(
            HttpStatusCode.UnprocessableEntity,
            $"expected 422 but got {(int)response.StatusCode} with body: {responseBody}");

        var problem = JsonSerializer.Deserialize<ApiProblemDetails>(responseBody, JsonOptions);
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status422UnprocessableEntity);

        return problem;
    }

    public static async Task<ApiProblemDetails> AssertAlreadyPublishedAsync(HttpResponseMessage response)
    {
        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(
            HttpStatusCode.UnprocessableEntity,
            $"expected 422 but got {(int)response.StatusCode} with body: {responseBody}");

        var problem = JsonSerializer.Deserialize<ApiProblemDetails>(responseBody, JsonOptions);
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status422UnprocessableEntity);
        problem.Code.Should().Be("EVENT_NOT_PUBLISHABLE");

        return problem;
    }

    public static async Task AssertNotFoundAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    public static async Task AssertUnauthorizedAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
