using EventHub.Api.Mapping;
using EventHub.Application.Events.Commands;
using EventHub.Contracts.Events;
using MediatR;

namespace EventHub.Api.Endpoints;

internal sealed class EventCoverImageEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/api/events/{eventId}/cover-image", UploadCoverImage)
            .WithName("UploadCoverImage")
            .WithTags("Events")
            .RequireAuthorization()
            .DisableAntiforgery()
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<CoverImageResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

    private static async Task<IResult> UploadCoverImage(
        int eventId,
        IFormFile file,
        ISender sender)
    {
        await using var stream = file.OpenReadStream();
        var command = new SetCoverImageCommand(eventId, stream, file.ContentType, file.FileName);

        var result = await sender.Send(command);

        if (!result.IsSuccess)
        {
            return result.ToHttpResult();
        }

        return Results.Ok(new CoverImageResponse(result.Value!.CoverImageUrl));
    }
}
