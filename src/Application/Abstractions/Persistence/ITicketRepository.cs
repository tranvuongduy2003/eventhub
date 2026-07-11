using EventHub.Domain.Events;
using EventHub.Domain.Orders;
using EventHub.Domain.Tickets;

namespace EventHub.Application.Abstractions.Persistence;

public interface ITicketRepository
{
    Task AddRangeAsync(IReadOnlyCollection<Ticket> tickets, CancellationToken cancellationToken = default);

    Task<bool> ExistsByCodeAsync(TicketCode code, CancellationToken cancellationToken = default);

    Task<Ticket?> GetByCodeAsync(TicketCode code, CancellationToken cancellationToken = default);

    Task<Ticket?> GetByIdAsync(TicketId ticketId, CancellationToken cancellationToken = default);

    Task<Ticket?> GetByIdForEventAsync(
        TicketId ticketId,
        EventId eventId,
        CancellationToken cancellationToken = default);

    Task<List<Ticket>> GetByOrderIdAsync(OrderId orderId, CancellationToken cancellationToken = default);

    Task<List<Ticket>> GetByHolderEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);

    Task<List<Ticket>> SearchForCheckInAsync(
        EventId eventId,
        string searchTerm,
        int limit,
        CancellationToken cancellationToken = default);

    Task<(int CheckedIn, int TotalIssued)> GetDoorCountsAsync(
        EventId eventId,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(Ticket ticket, CancellationToken cancellationToken = default);

    Task UpdateRangeAsync(IReadOnlyCollection<Ticket> tickets, CancellationToken cancellationToken = default);
}
