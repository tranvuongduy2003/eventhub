using EventHub.Api.Http;
using EventHub.Api.Mapping;
using EventHub.Application.Payments.Commands;
using EventHub.Contracts.Payments;
using MediatR;

namespace EventHub.Api.Endpoints;

internal sealed class PaymentsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/orders/{orderId}/payments", StartPayment)
            .WithName("StartPayment")
            .WithTags("Payments")
            .RequireCompleteJsonBody<StartPaymentRequest>()
            .Accepts<StartPaymentRequest>("application/json")
            .Produces<StartPaymentResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        endpoints.MapPost("/api/payments/provider-notifications/succeeded", ConfirmPayment)
            .WithName("ConfirmPayment")
            .WithTags("Payments")
            .RequireCompleteJsonBody<PaymentProviderNotificationRequest>()
            .Accepts<PaymentProviderNotificationRequest>("application/json")
            .Produces<PaymentProviderNotificationResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        endpoints.MapPost("/api/payments/provider-notifications/failed", FailPayment)
            .WithName("FailPayment")
            .WithTags("Payments")
            .RequireCompleteJsonBody<PaymentProviderNotificationRequest>()
            .Accepts<PaymentProviderNotificationRequest>("application/json")
            .Produces<PaymentProviderNotificationResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> StartPayment(
        int orderId,
        StartPaymentRequest request,
        ISender sender)
    {
        var result = await sender.Send(new StartPaymentCommand(
            orderId,
            request.SuccessUrl,
            request.CancelUrl));

        if (!result.IsSuccess)
        {
            return result.ToHttpResult();
        }

        var payment = result.Value!;

        return Results.Json(
            new StartPaymentResponse(
                payment.PaymentId,
                payment.OrderId,
                payment.Amount,
                payment.Currency,
                payment.ProviderReference,
                payment.RedirectUrl),
            statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> ConfirmPayment(
        PaymentProviderNotificationRequest request,
        ISender sender)
    {
        var result = await sender.Send(new ConfirmPaymentCommand(request.ProviderReference));
        return ToNotificationResult(result);
    }

    private static async Task<IResult> FailPayment(
        PaymentProviderNotificationRequest request,
        ISender sender)
    {
        var result = await sender.Send(new FailPaymentCommand(request.ProviderReference));
        return ToNotificationResult(result);
    }

    private static IResult ToNotificationResult(
        EventHub.Application.Common.Result<PaymentNotificationResult> result)
    {
        if (!result.IsSuccess)
        {
            return result.ToHttpResult();
        }

        var notification = result.Value!;

        return Results.Ok(new PaymentProviderNotificationResponse(
            notification.PaymentId,
            notification.OrderId,
            notification.PaymentStatus,
            notification.OrderStatus,
            notification.Applied));
    }
}
