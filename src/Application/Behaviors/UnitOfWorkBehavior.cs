using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Common;
using EventHub.Application.Options;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventHub.Application.Behaviors;

public sealed class UnitOfWorkBehavior<TRequest, TResponse>(
    IUnitOfWork unitOfWork,
    IOptions<ConcurrencyOptions> concurrencyOptions,
    ILogger<UnitOfWorkBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not IUnitOfWorkRequest)
        {
            return await next(cancellationToken);
        }

        var maxAttempts = Math.Max(1, concurrencyOptions.Value.MaxRetryAttempts);
        var delayMs = Math.Max(0, concurrencyOptions.Value.BaseDelayMilliseconds);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var response = await next(cancellationToken);

                if (response is IResult { IsFailure: true })
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return response;
                }

                await unitOfWork.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return response;
            }
            catch (Exception ex) when (unitOfWork.IsConcurrencyConflict(ex) && attempt < maxAttempts)
            {
                await transaction.RollbackAsync(cancellationToken);
                var backoff = delayMs * attempt + Random.Shared.Next(0, delayMs);
                logger.LogWarning(
                    ex,
                    "Concurrency conflict on {RequestName}, retry {Attempt}/{MaxAttempts} after {BackoffMs}ms",
                    typeof(TRequest).Name,
                    attempt,
                    maxAttempts,
                    backoff);
                await Task.Delay(backoff, cancellationToken);
            }
            catch (Exception ex) when (unitOfWork.TryMapPersistenceException(ex, out var error))
            {
                await transaction.RollbackAsync(cancellationToken);
                return ResultFactory.CreateFailure<TResponse>(error!);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        return ResultFactory.CreateFailure<TResponse>(
            Error.Conflict("CONCURRENCY_CONFLICT", "The resource was modified by another operation. Please retry."));
    }
}
