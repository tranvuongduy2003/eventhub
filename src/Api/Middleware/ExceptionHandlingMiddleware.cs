using System.Text.Json;
using EventHub.Api.Common;
using EventHub.Application.Exceptions;
using EventHub.Domain.Exceptions;

namespace EventHub.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await WriteProblemAsync(context, ex);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception exception)
    {
        logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);

        var (status, code, title) = MapException(exception);

        var problem = new ApiProblemDetails
        {
            Type = ProblemDetailsMapping.ToTypeIdentifier(code),
            Title = title,
            Detail = title,
            Status = status,
            Code = code,
        };

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }

    private static (int Status, string Code, string Title) MapException(Exception exception) =>
        exception switch
        {
            NotFoundException notFound => (StatusCodes.Status404NotFound, notFound.Code ?? "NOT_FOUND", notFound.Message),
            UnauthorizedApplicationException unauthorized => (
                StatusCodes.Status401Unauthorized,
                unauthorized.Code ?? "UNAUTHORIZED",
                unauthorized.Message),
            ConcurrencyConflictException => (
                StatusCodes.Status409Conflict,
                "CONCURRENCY_CONFLICT",
                exception.Message),
            BusinessRuleValidationException domain => (
                StatusCodes.Status422UnprocessableEntity,
                domain.Code ?? "BUSINESS_RULE_VIOLATION",
                domain.Message),
            DomainException domain => (
                StatusCodes.Status422UnprocessableEntity,
                domain.Code ?? "DOMAIN_ERROR",
                domain.Message),
            Application.Exceptions.ApplicationException app => (
                StatusCodes.Status422UnprocessableEntity,
                app.Code ?? "APPLICATION_ERROR",
                app.Message),
            _ => (StatusCodes.Status500InternalServerError, "INTERNAL_ERROR", "An unexpected error occurred."),
        };
}
