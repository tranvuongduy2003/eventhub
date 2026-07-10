using EventHub.Domain.Orders;
using EventHub.Domain.Tickets;

namespace EventHub.Application.Abstractions.Persistence;

public interface ITicketRepository
{
    Task AddRangeAsync(IReadOnlyCollection<Ticket> tickets, CancellationToken cancellationToken = default);

    Task<bool> ExistsByCodeAsync(TicketCode code, CancellationToken cancellationToken = default);

    Task<List<Ticket>> GetByOrderIdAsync(OrderId orderId, CancellationToken cancellationToken = default);

    Task<List<Ticket>> GetByHolderEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);

    Task UpdateRangeAsync(IReadOnlyCollection<Ticket> tickets, CancellationToken cancellationToken = default);
}
