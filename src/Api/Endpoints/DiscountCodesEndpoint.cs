using EventHub.Api.Http;
using EventHub.Api.Mapping;
using EventHub.Application.DiscountCodes.Commands;
using EventHub.Application.DiscountCodes.Queries;
using EventHub.Contracts.DiscountCodes;
using EventHub.Domain.DiscountCodes;
using MediatR;

namespace EventHub.Api.Endpoints;

internal sealed class DiscountCodesEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/events/{eventId}/discount-codes", CreateDiscountCode)
            .WithName("CreateDiscountCode")
            .WithTags("DiscountCodes")
            .RequireAuthorization()
            .RequireCompleteJsonBody<CreateDiscountCodeRequest>()
            .Accepts<CreateDiscountCodeRequest>("application/json")
            .Produces<DiscountCodeResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        endpoints.MapGet("/api/events/{eventId}/discount-codes", GetDiscountCodes)
            .WithName("GetDiscountCodes")
            .WithTags("DiscountCodes")
            .RequireAuthorization()
            .Produces<List<DiscountCodeResponse>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        endpoints.MapPut("/api/events/{eventId}/discount-codes/{discountCodeId}", UpdateDiscountCode)
            .WithName("UpdateDiscountCode")
            .WithTags("DiscountCodes")
            .RequireAuthorization()
            .RequireCompleteJsonBody<UpdateDiscountCodeRequest>()
            .Accepts<UpdateDiscountCodeRequest>("application/json")
            .Produces<DiscountCodeResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        endpoints.MapDelete("/api/events/{eventId}/discount-codes/{discountCodeId}", DeleteDiscountCode)
            .WithName("DeleteDiscountCode")
            .WithTags("DiscountCodes")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        endpoints.MapPost("/api/events/{eventId}/discount-codes/validate", ValidateDiscountCode)
            .WithName("ValidateDiscountCode")
            .WithTags("DiscountCodes")
            .RequireCompleteJsonBody<ValidateDiscountCodeRequest>()
            .Accepts<ValidateDiscountCodeRequest>("application/json")
            .Produces<DiscountCodeValidationResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> CreateDiscountCode(
        int eventId,
        CreateDiscountCodeRequest request,
        ISender sender)
    {
        DiscountCodeType.TryParse<DiscountCodeType>(request.Type, ignoreCase: true, out var type);
        if (type == default && request.Type != "Percentage")
        {
            type = DiscountCodeType.Percentage;
        }

        var command = new CreateDiscountCodeCommand(
            eventId,
            request.Code,
            type,
            request.Value,
            request.StartAt,
            request.EndAt,
            request.UsageCap);

        var result = await sender.Send(command);

        if (!result.IsSuccess)
        {
            return result.ToHttpResult();
        }

        var discountCode = result.Value!;

        return Results.Json(
            new DiscountCodeResponse(
                discountCode.DiscountCodeId,
                discountCode.EventId,
                discountCode.Code,
                discountCode.Type,
                discountCode.Value,
                discountCode.StartAt,
                discountCode.EndAt,
                discountCode.UsageCap,
                discountCode.UsedCount,
                IsDiscountCodeActive(discountCode.StartAt, discountCode.EndAt, discountCode.UsageCap, discountCode.UsedCount),
                discountCode.CreatedAt,
                null),
            statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> GetDiscountCodes(
        int eventId,
        ISender sender)
    {
        var query = new GetDiscountCodesQuery(eventId);
        var result = await sender.Send(query);

        if (!result.IsSuccess)
        {
            return result.ToHttpResult();
        }

        var discountCodes = result.Value!;

        return Results.Ok(discountCodes.Select(dc => new DiscountCodeResponse(
            dc.DiscountCodeId,
            dc.EventId,
            dc.Code,
            dc.Type,
            dc.Value,
            dc.StartAt,
            dc.EndAt,
            dc.UsageCap,
            dc.UsedCount,
            dc.IsActive,
            dc.CreatedAt,
            dc.UpdatedAt)).ToList());
    }

    private static async Task<IResult> UpdateDiscountCode(
        int eventId,
        int discountCodeId,
        UpdateDiscountCodeRequest request,
        ISender sender)
    {
        DiscountCodeType.TryParse<DiscountCodeType>(request.Type, ignoreCase: true, out var type);
        if (type == default && request.Type != "Percentage")
        {
            type = DiscountCodeType.Percentage;
        }

        var command = new UpdateDiscountCodeCommand(
            eventId,
            discountCodeId,
            type,
            request.Value,
            request.StartAt,
            request.EndAt,
            request.UsageCap);

        var result = await sender.Send(command);

        if (!result.IsSuccess)
        {
            return result.ToHttpResult();
        }

        var discountCode = result.Value!;

        return Results.Ok(
            new DiscountCodeResponse(
                discountCode.DiscountCodeId,
                discountCode.EventId,
                discountCode.Code,
                discountCode.Type,
                discountCode.Value,
                discountCode.StartAt,
                discountCode.EndAt,
                discountCode.UsageCap,
                discountCode.UsedCount,
                IsDiscountCodeActive(discountCode.StartAt, discountCode.EndAt, discountCode.UsageCap, discountCode.UsedCount),
                discountCode.CreatedAt,
                discountCode.UpdatedAt));
    }

    private static async Task<IResult> DeleteDiscountCode(
        int eventId,
        int discountCodeId,
        ISender sender)
    {
        var command = new DeleteDiscountCodeCommand(eventId, discountCodeId);

        var result = await sender.Send(command);

        if (!result.IsSuccess)
        {
            return result.ToHttpResult();
        }

        return Results.StatusCode(StatusCodes.Status204NoContent);
    }

    private static async Task<IResult> ValidateDiscountCode(
        int eventId,
        ValidateDiscountCodeRequest request,
        ISender sender)
    {
        var query = new ValidateDiscountCodeQuery(
            eventId,
            request.Code,
            request.OrderTotalAmount,
            request.OrderTotalCurrency);

        var result = await sender.Send(query);

        if (!result.IsSuccess)
        {
            return result.ToHttpResult();
        }

        var validation = result.Value!;

        return Results.Ok(
            new DiscountCodeValidationResponse(
                validation.DiscountCodeId,
                validation.Code,
                validation.Type,
                validation.Value,
                validation.DiscountAmount,
                validation.FinalTotal,
                validation.Currency));
    }

    private static bool IsDiscountCodeActive(
        DateTimeOffset? startAt,
        DateTimeOffset? endAt,
        int? usageCap,
        int usedCount)
    {
        var now = DateTimeOffset.UtcNow;
        if (startAt.HasValue && now < startAt.Value) return false;
        if (endAt.HasValue && now > endAt.Value) return false;
        if (usageCap.HasValue && usedCount >= usageCap.Value) return false;
        return true;
    }
}
