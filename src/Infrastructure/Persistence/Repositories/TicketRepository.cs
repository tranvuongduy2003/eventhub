using EventHub.Application.Abstractions.Persistence;
using EventHub.Domain.Events;
using EventHub.Domain.Orders;
using EventHub.Domain.Tickets;
using EventHub.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Infrastructure.Persistence.Repositories;

internal sealed class TicketRepository(ApplicationDatabaseContext databaseContext) : ITicketRepository
{
    public async Task AddRangeAsync(IReadOnlyCollection<Ticket> tickets, CancellationToken cancellationToken = default)
    {
        var records = tickets.Select(TicketPersistenceMapper.ToRecord).ToList();
        await databaseContext.Tickets.AddRangeAsync(records, cancellationToken);
    }

    public Task<bool> ExistsByCodeAsync(TicketCode code, CancellationToken cancellationToken = default) =>
        databaseContext.Tickets.AnyAsync(ticket => ticket.Code == code.Value, cancellationToken);

    public async Task<Ticket?> GetByCodeAsync(TicketCode code, CancellationToken cancellationToken = default)
    {
        var record = await databaseContext.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(ticket => ticket.Code == code.Value, cancellationToken);

        return record is null ? null : TicketPersistenceMapper.ToDomain(record);
    }

    public async Task<Ticket?> GetByIdForEventAsync(
        TicketId ticketId,
        EventId eventId,
        CancellationToken cancellationToken = default)
    {
        var record = await databaseContext.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(
                ticket => ticket.Id == ticketId.Value && ticket.EventId == eventId.Value,
                cancellationToken);

        return record is null ? null : TicketPersistenceMapper.ToDomain(record);
    }

    public async Task<List<Ticket>> GetByOrderIdAsync(
        OrderId orderId,
        CancellationToken cancellationToken = default)
    {
        var records = await databaseContext.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.OrderId == orderId.Value)
            .OrderBy(ticket => ticket.Id)
            .ToListAsync(cancellationToken);

        return records.Select(TicketPersistenceMapper.ToDomain).ToList();
    }

    public async Task<List<Ticket>> GetByHolderEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default)
    {
        var records = await databaseContext.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.HolderEmail == normalizedEmail)
            .OrderBy(ticket => ticket.IssuedAt)
            .ThenBy(ticket => ticket.Id)
            .ToListAsync(cancellationToken);

        return records.Select(TicketPersistenceMapper.ToDomain).ToList();
    }

    public async Task<List<Ticket>> SearchForCheckInAsync(
        EventId eventId,
        string searchTerm,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var normalizedSearchTerm = searchTerm.Trim().ToLowerInvariant();

        var records = await databaseContext.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.EventId == eventId.Value
                && (ticket.Code.ToLower().Contains(normalizedSearchTerm)
                    || ticket.HolderEmail.Contains(normalizedSearchTerm)))
            .OrderBy(ticket => ticket.HolderEmail)
            .ThenBy(ticket => ticket.IssuedAt)
            .ThenBy(ticket => ticket.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return records.Select(TicketPersistenceMapper.ToDomain).ToList();
    }

    public async Task<(int CheckedIn, int TotalIssued)> GetDoorCountsAsync(
        EventId eventId,
        CancellationToken cancellationToken = default)
    {
        var eventTickets = databaseContext.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.EventId == eventId.Value);

        var checkedIn = await eventTickets
            .CountAsync(ticket => ticket.Status == TicketStatus.CheckedIn.ToString(), cancellationToken);
        var totalIssued = await eventTickets.CountAsync(cancellationToken);

        return (checkedIn, totalIssued);
    }

    public Task UpdateAsync(Ticket ticket, CancellationToken cancellationToken = default)
    {
        var record = TicketPersistenceMapper.ToRecord(ticket);
        databaseContext.Tickets.Update(record);
        return Task.CompletedTask;
    }

    public Task UpdateRangeAsync(
        IReadOnlyCollection<Ticket> tickets,
        CancellationToken cancellationToken = default)
    {
        var records = tickets.Select(TicketPersistenceMapper.ToRecord).ToList();
        databaseContext.Tickets.UpdateRange(records);
        return Task.CompletedTask;
    }
}
