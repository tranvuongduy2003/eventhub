using EventHub.Api.Mapping;
using EventHub.Application.Events.Queries;
using EventHub.Contracts.Events;
using EventHub.Domain.Events;
using MediatR;

namespace EventHub.Api.Endpoints;

internal sealed class AuditLogEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/events/{eventId}/audit-log", ListAuditLog)
            .WithName("ListAuditLog")
            .WithTags("AuditLog")
            .RequireAuthorization()
            .Produces<AuditLogResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> ListAuditLog(
        int eventId,
        ISender sender,
        int? page,
        int? pageSize,
        DateTimeOffset? from,
        DateTimeOffset? to,
        string? action)
    {
        AuditAction? parsedAction = null;
        if (!string.IsNullOrEmpty(action)
            && Enum.TryParse<AuditAction>(action, ignoreCase: true, out var parsed))
        {
            parsedAction = parsed;
        }

        var result = await sender.Send(new ListAuditLogQuery(
            eventId,
            page ?? 1,
            pageSize ?? 20,
            from,
            to,
            parsedAction));

        if (!result.IsSuccess)
        {
            return result.ToHttpResult();
        }

        return Results.Ok(result.Value);
    }
}
