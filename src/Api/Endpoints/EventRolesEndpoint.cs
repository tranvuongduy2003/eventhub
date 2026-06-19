using EventHub.Api.Http;
using EventHub.Api.Mapping;
using EventHub.Application.Events.Commands;
using EventHub.Application.Events.Queries;
using EventHub.Contracts.Events;
using MediatR;

namespace EventHub.Api.Endpoints;

internal sealed class EventRolesEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/events/{eventId}/roles", AssignRole)
            .WithName("AssignRole")
            .WithTags("EventRoles")
            .RequireAuthorization()
            .RequireCompleteJsonBody<AssignRoleRequest>()
            .Accepts<AssignRoleRequest>("application/json")
            .Produces<EventRoleAssignmentResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        endpoints.MapDelete("/api/events/{eventId}/roles/{userId:guid}", RevokeRole)
            .WithName("RevokeRole")
            .WithTags("EventRoles")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        endpoints.MapGet("/api/events/{eventId}/roles", ListRoles)
            .WithName("ListEventRoleAssignments")
            .WithTags("EventRoles")
            .RequireAuthorization()
            .Produces<IReadOnlyList<EventRoleAssignmentResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> AssignRole(
        int eventId,
        AssignRoleRequest request,
        ISender sender)
    {
        var result = await sender.Send(
            new AssignRoleCommand(eventId, request.UserId, request.Role));

        if (!result.IsSuccess)
        {
            return result.ToHttpResult();
        }

        var assignment = result.Value!;

        return Results.Created(
            $"/api/events/{eventId}/roles/{assignment.UserId:D}",
            new EventRoleAssignmentResponse(
                assignment.UserId,
                assignment.DisplayName,
                assignment.Email,
                assignment.Role));
    }

    private static async Task<IResult> RevokeRole(
        int eventId,
        Guid userId,
        ISender sender)
    {
        var result = await sender.Send(new RevokeRoleCommand(eventId, userId));

        if (!result.IsSuccess)
        {
            return result.ToHttpResult();
        }

        return Results.NoContent();
    }

    private static async Task<IResult> ListRoles(
        int eventId,
        ISender sender)
    {
        var result = await sender.Send(new ListEventRoleAssignmentsQuery(eventId));

        if (!result.IsSuccess)
        {
            return result.ToHttpResult();
        }

        return Results.Ok(result.Value);
    }
}
