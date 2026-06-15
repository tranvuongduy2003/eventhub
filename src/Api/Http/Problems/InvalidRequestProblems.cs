using System.Text.Json;
using EventHub.Api.Common;

namespace EventHub.Api.Http.Problems;

internal static class InvalidRequestProblems
{
    private const string Title = "The request body is invalid.";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IResult AsResult() => Results.Problem(CreateDetails());

    public static ApiProblemDetails CreateDetails() =>
        new()
        {
            Type = ProblemDetailsMapping.ToTypeIdentifier(ApiErrorCodes.InvalidRequest),
            Title = Title,
            Detail = Title,
            Status = StatusCodes.Status400BadRequest,
            Code = ApiErrorCodes.InvalidRequest,
        };

    public static async Task WriteAsync(HttpContext context)
    {
        if (context.Response.HasStarted)
        {
            throw new InvalidOperationException("The response has already started.");
        }

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(CreateDetails(), JsonOptions));
    }
}
