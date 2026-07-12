using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Reporting;
using EventHub.Domain.Events;
using EventHub.Domain.Orders;
using EventHub.Domain.Tickets;
using EventHub.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Infrastructure.Persistence.Repositories;

internal sealed class ReportingRepository(ApplicationDatabaseContext databaseContext) : IReportingRepository
{
    public async Task<IReadOnlyList<EventAttendeeResult>> ListAttendeesAsync(
        EventId eventId,
        CancellationToken cancellationToken = default)
    {
        var query =
            from ticket in databaseContext.Tickets.AsNoTracking()
            join ticketType in databaseContext.TicketTypes.AsNoTracking()
                on ticket.TicketTypeId equals ticketType.Id
            join order in databaseContext.Orders.AsNoTracking()
                on ticket.OrderId equals order.Id
            where ticket.EventId == eventId.Value && order.Status == OrderStatus.Confirmed.ToString()
            orderby ticket.Status == TicketStatus.CheckedIn.ToString() descending,
                ticket.HolderEmail,
                ticket.IssuedAt,
                ticket.Id
            select new EventAttendeeResult(
                ticket.HolderName,
                ticket.HolderEmail,
                ticket.TicketTypeId,
                ticketType.Name,
                ticket.OrderId,
                ticket.Id,
                ticket.Status == TicketStatus.CheckedIn.ToString(),
                ticket.CheckedInAt);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<EventResultsResult> GetEventResultsAsync(
        EventId eventId,
        CancellationToken cancellationToken = default)
    {
        var eventRecord = await databaseContext.Events
            .AsNoTracking()
            .Where(eventItem => eventItem.Id == eventId.Value)
            .Select(eventItem => new { eventItem.Id, eventItem.Title })
            .FirstOrDefaultAsync(cancellationToken);

        var confirmedOrders = databaseContext.Orders
            .AsNoTracking()
            .Where(order => order.EventId == eventId.Value && order.Status == OrderStatus.Confirmed.ToString());

        var revenue = await confirmedOrders
            .GroupBy(order => order.TotalCurrency)
            .Select(group => new
            {
                Currency = group.Key,
                Amount = group.Sum(order => order.TotalAmount),
            })
            .FirstOrDefaultAsync(cancellationToken);

        var tickets = databaseContext.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.EventId == eventId.Value);

        var issuedCount = await tickets.CountAsync(cancellationToken);
        var checkedInCount = await tickets
            .CountAsync(ticket => ticket.Status == TicketStatus.CheckedIn.ToString(), cancellationToken);

        var ticketTypeRevenueByType = await (
            from line in databaseContext.OrderLines.AsNoTracking()
            join order in confirmedOrders on line.OrderId equals order.Id
            join ticketType in databaseContext.TicketTypes.AsNoTracking()
                on line.TicketTypeId equals ticketType.Id
            group new { line, ticketType } by new
            {
                line.TicketTypeId,
                ticketType.Name,
                line.LineTotalCurrency,
            }
            into sales
            select new
            {
                sales.Key.TicketTypeId,
                SoldCount = sales.Sum(item => item.line.Quantity),
                RevenueAmount = sales.Sum(item => item.line.LineTotalAmount),
                RevenueCurrency = sales.Key.LineTotalCurrency,
            })
            .ToListAsync(cancellationToken);

        var revenueByTicketType = ticketTypeRevenueByType.ToDictionary(
            item => item.TicketTypeId,
            item => item);

        var ticketsSoldByType = await databaseContext.TicketTypes
            .AsNoTracking()
            .Where(ticketType => ticketType.EventId == eventId.Value)
            .OrderBy(ticketType => ticketType.Name)
            .Select(ticketType => new
            {
                ticketType.Id,
                ticketType.Name,
                ticketType.Capacity,
                ticketType.Sold,
                ticketType.Reserved,
                ticketType.PriceCurrency,
            })
            .ToListAsync(cancellationToken);

        return new EventResultsResult(
            eventId.Value,
            eventRecord?.Title ?? string.Empty,
            revenue?.Amount ?? 0m,
            revenue?.Currency ?? "VND",
            issuedCount,
            checkedInCount,
            issuedCount - checkedInCount,
            issuedCount == 0 ? 0m : decimal.Round((decimal)checkedInCount / issuedCount, 4),
            ticketsSoldByType.Select(ticketType =>
            {
                var hasRevenue = revenueByTicketType.TryGetValue(ticketType.Id, out var revenue);
                var remainingCount = Math.Max(0, ticketType.Capacity - ticketType.Sold - ticketType.Reserved);

                return new TicketTypeSalesResult(
                    ticketType.Id,
                    ticketType.Name,
                    ticketType.Capacity,
                    ticketType.Sold,
                    ticketType.Reserved,
                    remainingCount,
                    hasRevenue ? revenue!.RevenueAmount : 0m,
                    hasRevenue ? revenue!.RevenueCurrency : ticketType.PriceCurrency);
            }).ToList());
    }

    public async Task<OrganizerAudienceOverviewResult> GetOrganizerOverviewAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var ownedEvents = await databaseContext.Events
            .AsNoTracking()
            .Where(eventItem => eventItem.OrganizerId == userId)
            .OrderByDescending(eventItem => eventItem.ScheduleStartsAt ?? eventItem.UpdatedAt)
            .Select(eventItem => new
            {
                eventItem.Id,
                eventItem.Title,
                eventItem.Status,
                eventItem.ScheduleStartsAt,
                eventItem.ScheduleTimeZoneId,
            })
            .ToListAsync(cancellationToken);

        var staffEvents = await (
            from role in databaseContext.EventUserRoles.AsNoTracking()
            join eventItem in databaseContext.Events.AsNoTracking() on role.EventId equals eventItem.Id
            where role.UserId == userId && role.Role == EventRole.Staff
            orderby eventItem.ScheduleStartsAt ?? eventItem.UpdatedAt descending
            select new
            {
                eventItem.Id,
                eventItem.Title,
                eventItem.Status,
                eventItem.ScheduleStartsAt,
                eventItem.ScheduleTimeZoneId,
            })
            .ToListAsync(cancellationToken);

        var ownedResults = new List<OwnedEventOverviewResult>();
        foreach (var eventItem in ownedEvents)
        {
            var results = await GetEventResultsAsync(EventId.From(eventItem.Id), cancellationToken);
            ownedResults.Add(new OwnedEventOverviewResult(
                eventItem.Id,
                eventItem.Title,
                eventItem.Status.ToString(),
                eventItem.ScheduleStartsAt,
                eventItem.ScheduleTimeZoneId,
                results.IssuedCount,
                results.TotalRevenueAmount,
                results.TotalRevenueCurrency,
                results.CheckedInCount,
                results.IssuedCount));
        }

        var staffResults = new List<StaffEventOverviewResult>();
        foreach (var eventItem in staffEvents)
        {
            var results = await GetEventResultsAsync(EventId.From(eventItem.Id), cancellationToken);
            staffResults.Add(new StaffEventOverviewResult(
                eventItem.Id,
                eventItem.Title,
                eventItem.Status.ToString(),
                eventItem.ScheduleStartsAt,
                eventItem.ScheduleTimeZoneId,
                results.CheckedInCount,
                results.IssuedCount));
        }

        return new OrganizerAudienceOverviewResult(ownedResults, staffResults);
    }

    public async Task<EventReminderSettingsResult> SetReminderSettingsAsync(
        EventId eventId,
        bool enabled,
        int leadTimeMinutes,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken = default)
    {
        var reminder = await databaseContext.EventReminders
            .FirstOrDefaultAsync(item => item.EventId == eventId.Value, cancellationToken);

        if (reminder is null)
        {
            reminder = new EventReminderRecord
            {
                EventId = eventId.Value,
                Enabled = enabled,
                LeadTimeMinutes = leadTimeMinutes,
                UpdatedAt = updatedAt,
            };
            await databaseContext.EventReminders.AddAsync(reminder, cancellationToken);
        }
        else
        {
            reminder.Enabled = enabled;
            reminder.LeadTimeMinutes = leadTimeMinutes;
            reminder.UpdatedAt = updatedAt;
            reminder.LastSentAt = null;
            databaseContext.EventReminders.Update(reminder);
        }

        return new EventReminderSettingsResult(
            reminder.EventId,
            reminder.Enabled,
            reminder.LeadTimeMinutes,
            reminder.UpdatedAt,
            reminder.LastSentAt);
    }

    public async Task<IReadOnlyList<DueEventReminderResult>> GetDueRemindersAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var reminders = await (
            from reminder in databaseContext.EventReminders.AsNoTracking()
            join eventItem in databaseContext.Events.AsNoTracking() on reminder.EventId equals eventItem.Id
            where reminder.Enabled
                && reminder.LastSentAt == null
                && eventItem.ScheduleStartsAt != null
            select new
            {
                reminder.EventId,
                reminder.LeadTimeMinutes,
                eventItem.Title,
                StartsAt = eventItem.ScheduleStartsAt!.Value,
                eventItem.ScheduleTimeZoneId,
            })
            .ToListAsync(cancellationToken);

        return reminders
            .Where(reminder => reminder.StartsAt.AddMinutes(-reminder.LeadTimeMinutes) <= now)
            .Select(reminder => new DueEventReminderResult(
                reminder.EventId,
                reminder.Title,
                reminder.StartsAt,
                reminder.ScheduleTimeZoneId,
                reminder.LeadTimeMinutes))
            .ToList();
    }

    public async Task MarkReminderSentAsync(
        EventId eventId,
        DateTimeOffset sentAt,
        CancellationToken cancellationToken = default)
    {
        var reminder = await databaseContext.EventReminders
            .FirstOrDefaultAsync(item => item.EventId == eventId.Value, cancellationToken);

        if (reminder is null)
        {
            return;
        }

        reminder.LastSentAt = sentAt;
        reminder.UpdatedAt = sentAt;
        databaseContext.EventReminders.Update(reminder);
    }
}
