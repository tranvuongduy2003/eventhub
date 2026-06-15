using System.Text.Json;
using EventHub.Api.Http.Problems;
using Microsoft.AspNetCore.Http;

namespace EventHub.Api.Middleware;

public sealed class InvalidRequestMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception) when (IsInvalidRequestException(exception))
        {
            await InvalidRequestProblems.WriteAsync(context);
        }
    }

    private static bool IsInvalidRequestException(Exception exception) =>
        exception is JsonException or BadHttpRequestException;
}
