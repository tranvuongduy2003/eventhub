using EventHub.Api.Auth;
using EventHub.Api.Http;
using EventHub.Api.Mapping;
using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Options;
using EventHub.Application.Users.Commands;
using EventHub.Application.Users.Queries;
using EventHub.Contracts.Users;
using MediatR;
using Microsoft.Extensions.Options;

namespace EventHub.Api.Endpoints;

internal sealed class AuthEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/auth/login", LoginUser)
            .WithName("LoginUser")
            .WithTags("Auth")
            .AllowAnonymous()
            .RequireCompleteJsonBody<LoginUserRequest>()
            .Accepts<LoginUserRequest>("application/json")
            .Produces<LoginUserResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        endpoints.MapPost("/api/auth/logout", LogoutUser)
            .WithName("LogoutUser")
            .WithTags("Auth")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        endpoints.MapGet("/api/auth/me", GetCurrentUser)
            .WithName("GetCurrentUser")
            .WithTags("Auth")
            .RequireAuthorization()
            .Produces<LoginUserResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> LoginUser(
        LoginUserRequest request,
        ISender sender,
        HttpContext httpContext,
        IOptions<AuthSessionOptions> sessionOptions)
    {
        var result = await sender.Send(new LoginUserCommand(request.Email, request.Password));

        if (!result.IsSuccess)
        {
            return result.ToHttpResult();
        }

        var loginUser = result.Value!;
        SessionCookieWriter.Append(
            httpContext,
            loginUser.SessionId,
            loginUser.SessionExpiresAt,
            sessionOptions.Value);

        return Results.Ok(
            new LoginUserResponse(loginUser.UserId, loginUser.DisplayName, loginUser.Email));
    }

    private static async Task<IResult> LogoutUser(
        HttpContext httpContext,
        ISender sender,
        ICurrentUserAccessor currentUserAccessor,
        IOptions<AuthSessionOptions> sessionOptions)
    {
        if (!httpContext.Request.Cookies.TryGetValue(
                sessionOptions.Value.CookieName,
                out var sessionIdValue)
            || !Guid.TryParse(sessionIdValue, out var sessionId)
            || currentUserAccessor.UserId is null)
        {
            return Results.Unauthorized();
        }

        var result = await sender.Send(
            new LogoutUserCommand(sessionId, currentUserAccessor.UserId.Value.Value));

        if (!result.IsSuccess)
        {
            return result.ToHttpResult();
        }

        SessionCookieWriter.Delete(httpContext, sessionOptions.Value);

        return Results.NoContent();
    }

    private static async Task<IResult> GetCurrentUser(ISender sender)
    {
        var result = await sender.Send(new GetCurrentUserQuery());

        if (!result.IsSuccess)
        {
            return result.ToHttpResult();
        }

        var currentUser = result.Value!;
        return Results.Ok(new LoginUserResponse(
            currentUser.UserId,
            currentUser.DisplayName,
            currentUser.Email));
    }
}
