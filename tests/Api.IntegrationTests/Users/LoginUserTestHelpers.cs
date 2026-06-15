using System.Net;
using System.Text.Json;
using EventHub.Api.Common;
using EventHub.Application.Common;
using EventHub.Application.Users;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace EventHub.Api.IntegrationTests.Users;

internal static class LoginUserTestHelpers
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<ApiProblemDetails> AssertInvalidCredentialsAsync(HttpResponseMessage response)
    {
        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(
            HttpStatusCode.Unauthorized,
            $"expected 401 but got {(int)response.StatusCode} with body: {responseBody}");

        response.Headers.Contains("Set-Cookie").Should().BeFalse(
            "failed login must not issue a session cookie");

        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var problem = JsonSerializer.Deserialize<ApiProblemDetails>(responseBody, JsonOptions);
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status401Unauthorized);
        problem.Code.Should().Be(LoginErrors.InvalidCredentialsCode);
        problem.Detail.Should().Contain("Email or password is incorrect");

        return problem;
    }

    public static Task<ApiProblemDetails> AssertValidationFailedAsync(
        HttpResponseMessage response,
        string expectedCode = Error.ValidationFailedCode,
        Action<IReadOnlyDictionary<string, string[]>>? assertErrors = null) =>
        RegisterUserTestHelpers.AssertValidationFailedAsync(response, expectedCode, errors =>
        {
            response.Headers.Contains("Set-Cookie").Should().BeFalse(
                "validation failure must not issue a session cookie");
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
            assertErrors?.Invoke(errors);
        });
}
