using EventHub.Api.Auth;
using EventHub.Api.Http;
using EventHub.Api.Mapping;
using EventHub.Application.Options;
using EventHub.Application.Users.Commands;
using EventHub.Contracts.Users;
using MediatR;
using Microsoft.Extensions.Options;

namespace EventHub.Api.Endpoints;

internal sealed class AttendeesEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/api/attendees", RegisterAttendee)
            .WithName("RegisterAttendee")
            .WithTags("Attendees")
            .AllowAnonymous()
            .RequireCompleteJsonBody<RegisterUserRequest>()
            .Accepts<RegisterUserRequest>("application/json")
            .Produces<UserRegistrationResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

    private static async Task<IResult> RegisterAttendee(
        RegisterUserRequest request,
        ISender sender,
        HttpContext httpContext,
        IOptions<AuthSessionOptions> sessionOptions)
    {
        var result = await sender.Send(
            new RegisterAttendeeCommand(request.DisplayName, request.Email, request.Password));

        if (!result.IsSuccess)
        {
            return result.ToHttpResult();
        }

        var registeredUser = result.Value!;
        SessionCookieWriter.Append(
            httpContext,
            registeredUser.SessionId,
            registeredUser.SessionExpiresAt,
            sessionOptions.Value);

        return Results.Created(
            $"/api/attendees/{registeredUser.UserId:D}",
            new UserRegistrationResponse(
                registeredUser.UserId,
                registeredUser.DisplayName,
                registeredUser.Email,
                registeredUser.CreatedAt));
    }
}
