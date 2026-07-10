using EventHub.Application.Reporting.Commands;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventHub.Infrastructure.BackgroundJobs;

internal sealed class EventReminderDispatchJob(
    IServiceProvider serviceProvider,
    ILogger<EventReminderDispatchJob> logger)
    : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Event reminder dispatch job started.");

        using var timer = new PeriodicTimer(PollingInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueReminders(stoppingToken);
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error dispatching event reminders.");
            }
        }

        logger.LogInformation("Event reminder dispatch job stopped.");
    }

    private async Task ProcessDueReminders(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var result = await sender.Send(new ProcessDueEventRemindersCommand(), cancellationToken);

        if (result is { IsSuccess: true, Value: { EventCount: > 0 } value })
        {
            logger.LogInformation(
                "Dispatched reminders for {EventCount} events to {RecipientCount} recipients.",
                value.EventCount,
                value.RecipientCount);
        }
    }
}
