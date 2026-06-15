using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EventHub.Application.Behaviors;

public sealed class PostCommitSessionCacheBehavior<TRequest, TResponse>(
    IPendingSessionCacheCollector pendingSessionCacheCollector,
    ISessionStore sessionStore,
    ILogger<PostCommitSessionCacheBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next(cancellationToken);

        if (response is not IResult { IsSuccess: true })
        {
            return response;
        }

        var pendingSessionCaches = pendingSessionCacheCollector.Drain();
        foreach (var entry in pendingSessionCaches)
        {
            try
            {
                await sessionStore.TryWriteCacheAsync(entry, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "Failed to write session {SessionId} to cache after commit",
                    entry.SessionId);
            }
        }

        return response;
    }
}
