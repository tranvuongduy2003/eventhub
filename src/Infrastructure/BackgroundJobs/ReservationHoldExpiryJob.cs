using EventHub.Application.Orders.Commands;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventHub.Infrastructure.BackgroundJobs;

internal sealed class ReservationHoldExpiryJob(
    IServiceProvider serviceProvider,
    ILogger<ReservationHoldExpiryJob> logger)
    : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Reservation hold expiry job started.");

        using var timer = new PeriodicTimer(PollingInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredReservations(stoppingToken);
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error processing expired reservations.");
            }
        }

        logger.LogInformation("Reservation hold expiry job stopped.");
    }

    private async Task ProcessExpiredReservations(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var result = await sender.Send(new ProcessExpiredOrderHoldsCommand(), cancellationToken);

        if (result is { IsSuccess: true, Value: { ExpiredOrderCount: > 0 } value })
        {
            logger.LogInformation(
                "Expired {OrderCount} orders and released {ReservationCount} reservations.",
                value.ExpiredOrderCount,
                value.ReleasedReservationCount);
        }
    }
}
