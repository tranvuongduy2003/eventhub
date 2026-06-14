using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Solution.Api.Http.Problems;

namespace Solution.Api.Middleware;

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
