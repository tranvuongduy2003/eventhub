using EventHub.Application.Common;
using EventHub.Application.Realtime;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EventHub.Application.Behaviors;

public sealed class PostCommitRealtimeSalesInventoryBehavior<TRequest, TResponse>(
    IPendingRealtimeSalesInventoryUpdateCollector pendingRealtimeSalesInventoryUpdateCollector,
    IPendingRealtimeCheckInUpdateCollector pendingRealtimeCheckInUpdateCollector,
    IRealtimeSalesInventoryNotifier realtimeSalesInventoryNotifier,
    IRealtimeCheckInNotifier realtimeCheckInNotifier,
    ILogger<PostCommitRealtimeSalesInventoryBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next(cancellationToken);

        if (response is IResult { IsSuccess: true })
        {
            await PublishRealtimeSalesInventoryUpdatesAsync(cancellationToken);
            await PublishRealtimeCheckInUpdatesAsync(cancellationToken);
        }

        return response;
    }

    private async Task PublishRealtimeSalesInventoryUpdatesAsync(CancellationToken cancellationToken)
    {
        var eventIds = pendingRealtimeSalesInventoryUpdateCollector.Drain();

        foreach (var eventId in eventIds)
        {
            try
            {
                await realtimeSalesInventoryNotifier.NotifySalesInventoryChangedAsync(
                    eventId,
                    cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(
                    exception,
                    "Realtime sales inventory update failed for event {EventId}",
                    eventId.Value);
            }
        }
    }

    private async Task PublishRealtimeCheckInUpdatesAsync(CancellationToken cancellationToken)
    {
        var eventIds = pendingRealtimeCheckInUpdateCollector.Drain();

        foreach (var eventId in eventIds)
        {
            try
            {
                await realtimeCheckInNotifier.NotifyCheckInChangedAsync(eventId, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(
                    exception,
                    "Realtime check-in update failed for event {EventId}",
                    eventId.Value);
            }
        }
    }
}
