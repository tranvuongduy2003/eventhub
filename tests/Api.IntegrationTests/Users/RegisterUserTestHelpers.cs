using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EventHub.Api.Common;
using EventHub.Application.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace EventHub.Api.IntegrationTests.Users;

internal static class RegisterUserTestHelpers
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<ApiProblemDetails> AssertValidationFailedAsync(
        HttpResponseMessage response,
        string expectedCode = Error.ValidationFailedCode,
        Action<IReadOnlyDictionary<string, string[]>>? assertErrors = null)
    {
        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(
            HttpStatusCode.UnprocessableEntity,
            $"expected 422 but got {(int)response.StatusCode} with body: {responseBody}");

        var problem = JsonSerializer.Deserialize<ApiProblemDetails>(responseBody, JsonOptions);
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status422UnprocessableEntity);
        problem.Code.Should().Be(expectedCode);

        if (assertErrors is not null)
        {
            var errors = ReadFieldErrors(problem);
            assertErrors(errors);
        }

        return problem;
    }

    public static async Task AssertInvalidRequestAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<ApiProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status400BadRequest);
        problem.Code.Should().Be("INVALID_REQUEST");
    }

    public static IReadOnlyDictionary<string, string[]> ReadFieldErrors(ApiProblemDetails problem)
    {
        problem.Extensions.Should().ContainKey("errors");
        var errorsElement = (JsonElement)problem.Extensions["errors"]!;
        return JsonSerializer.Deserialize<Dictionary<string, string[]>>(errorsElement.GetRawText(), JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize validation errors.");
    }

    public static StringContent JsonContent(string json) =>
        new(json, System.Text.Encoding.UTF8, "application/json");
}
