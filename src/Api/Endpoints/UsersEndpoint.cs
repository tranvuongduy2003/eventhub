using MediatR;
using Microsoft.Extensions.Options;
using Solution.Api.Auth;
using Solution.Api.Http;
using Solution.Api.Mapping;
using Solution.Application.Options;
using Solution.Application.Users.Commands;
using Solution.Contracts.Users;

namespace Solution.Api.Endpoints;

internal sealed class UsersEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/api/users", RegisterUser)
            .WithName("RegisterUser")
            .WithTags("Users")
            .AllowAnonymous()
            .RequireCompleteJsonBody<RegisterUserRequest>()
            .Accepts<RegisterUserRequest>("application/json")
            .Produces<UserRegistrationResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

    private static async Task<IResult> RegisterUser(
        RegisterUserRequest request,
        ISender sender,
        HttpContext httpContext,
        IOptions<AuthSessionOptions> sessionOptions)
    {
        var result = await sender.Send(
            new RegisterUserCommand(request.Username, request.Email, request.Password));

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
            $"/api/users/{registeredUser.UserId:D}",
            new UserRegistrationResponse(
                registeredUser.UserId,
                registeredUser.Username,
                registeredUser.Email,
                registeredUser.CreatedAt));
    }
}
