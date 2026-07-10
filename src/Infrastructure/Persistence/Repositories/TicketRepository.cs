using EventHub.Application.Abstractions.Persistence;
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

    public Task UpdateRangeAsync(
        IReadOnlyCollection<Ticket> tickets,
        CancellationToken cancellationToken = default)
    {
        var records = tickets.Select(TicketPersistenceMapper.ToRecord).ToList();
        databaseContext.Tickets.UpdateRange(records);
        return Task.CompletedTask;
    }
}
