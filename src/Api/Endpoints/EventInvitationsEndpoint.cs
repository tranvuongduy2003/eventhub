using EventHub.Api.Http;
using EventHub.Api.Mapping;
using EventHub.Application.Events.Commands;
using EventHub.Application.Events.Queries;
using EventHub.Contracts.Events;
using MediatR;

namespace EventHub.Api.Endpoints;

internal sealed class EventInvitationsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/events/{eventId}/invitations", SendInvitation)
            .WithName("SendInvitation")
            .WithTags("EventInvitations")
            .RequireAuthorization()
            .RequireCompleteJsonBody<SendInvitationRequest>()
            .Accepts<SendInvitationRequest>("application/json")
            .Produces<InvitationResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        endpoints.MapDelete("/api/events/{eventId}/invitations/{invitationId:int}", RevokeInvitation)
            .WithName("RevokeInvitation")
            .WithTags("EventInvitations")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        endpoints.MapGet("/api/events/{eventId}/invitations", ListInvitations)
            .WithName("ListInvitations")
            .WithTags("EventInvitations")
            .RequireAuthorization()
            .Produces<IReadOnlyList<InvitationResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        endpoints.MapPost("/api/invitations/{invitationId:int}/accept", AcceptInvitation)
            .WithName("AcceptInvitation")
            .WithTags("EventInvitations")
            .RequireAuthorization()
            .RequireCompleteJsonBody<AcceptInvitationRequest>()
            .Accepts<AcceptInvitationRequest>("application/json")
            .Produces<InvitationResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> SendInvitation(
        int eventId,
        SendInvitationRequest request,
        ISender sender)
    {
        var result = await sender.Send(
            new SendInvitationCommand(eventId, request.Email, request.ExpiresInDays));

        if (!result.IsSuccess)
        {
            return result.ToHttpResult();
        }

        var invitation = result.Value!;

        return Results.Created(
            $"/api/events/{eventId}/invitations/{invitation.InvitationId}",
            new InvitationResponse(
                invitation.InvitationId,
                invitation.Email,
                invitation.Status,
                invitation.CreatedAt,
                invitation.ExpiresAt,
                null,
                null,
                invitation.Token));
    }

    private static async Task<IResult> RevokeInvitation(
        int eventId,
        int invitationId,
        ISender sender)
    {
        var result = await sender.Send(new RevokeInvitationCommand(eventId, invitationId));

        if (!result.IsSuccess)
        {
            return result.ToHttpResult();
        }

        return Results.NoContent();
    }

    private static async Task<IResult> ListInvitations(
        int eventId,
        ISender sender)
    {
        var result = await sender.Send(new ListInvitationsQuery(eventId));

        if (!result.IsSuccess)
        {
            return result.ToHttpResult();
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> AcceptInvitation(
        int invitationId,
        AcceptInvitationRequest request,
        ISender sender)
    {
        var result = await sender.Send(
            new AcceptInvitationCommand(invitationId, request.Token));

        if (!result.IsSuccess)
        {
            return result.ToHttpResult();
        }

        var invitation = result.Value!;

        return Results.Ok(
            new InvitationResponse(
                invitation.InvitationId,
                invitation.Email,
                invitation.Status,
                DateTimeOffset.MinValue,
                DateTimeOffset.MinValue,
                null,
                null));
    }
}
