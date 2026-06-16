using EventHub.Api.Http;
using EventHub.Api.Mapping;
using EventHub.Application.Users.Commands;
using EventHub.Contracts.Users;
using MediatR;
using Microsoft.AspNetCore.Antiforgery;

namespace EventHub.Api.Endpoints;

internal sealed class ProfileEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPatch("/api/users/me", UpdateProfile)
            .WithName("UpdateProfile")
            .WithTags("Profile")
            .RequireAuthorization()
            .RequireCompleteJsonBody<UpdateProfileRequest>()
            .Accepts<UpdateProfileRequest>("application/json")
            .Produces<UpdateProfileResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        endpoints.MapPost("/api/users/me/avatar", UploadAvatar)
            .WithName("UploadAvatar")
            .WithTags("Profile")
            .RequireAuthorization()
            .DisableAntiforgery()
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<UploadAvatarResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        endpoints.MapDelete("/api/users/me/avatar", RemoveAvatar)
            .WithName("RemoveAvatar")
            .WithTags("Profile")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> UpdateProfile(
        UpdateProfileRequest request,
        ISender sender)
    {
        var result = await sender.Send(
            new UpdateProfileCommand(request.DisplayName, request.Email));

        if (!result.IsSuccess)
        {
            return result.ToHttpResult();
        }

        var profile = result.Value!;
        return Results.Ok(
            new UpdateProfileResponse(
                profile.UserId,
                profile.DisplayName,
                profile.Email,
                profile.AvatarUrl));
    }

    private static async Task<IResult> UploadAvatar(
        IFormFile file,
        ISender sender)
    {
        await using var stream = file.OpenReadStream();
        var result = await sender.Send(
            new UploadAvatarCommand(stream, file.ContentType, file.FileName));

        if (!result.IsSuccess)
        {
            return result.ToHttpResult();
        }

        return Results.Ok(new UploadAvatarResponse(result.Value!.AvatarUrl));
    }

    private static async Task<IResult> RemoveAvatar(ISender sender)
    {
        var result = await sender.Send(new RemoveAvatarCommand());

        if (!result.IsSuccess)
        {
            return result.ToHttpResult();
        }

        return Results.NoContent();
    }
}
