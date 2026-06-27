using EventHub.Domain.Events;
using EventHub.Infrastructure.Persistence.Entities;

namespace EventHub.Infrastructure.Persistence.Mapping;

internal static class TicketTypePersistenceMapper
{
    public static TicketTypeRecord ToRecord(TicketType domain, int eventId) =>
        new()
        {
            EventId = eventId,
            Name = domain.Name.Value,
            PriceAmount = domain.Price.Amount,
            PriceCurrency = domain.Price.Currency,
            Capacity = domain.Capacity.Value,
            MaxPerOrder = domain.MaxPerOrder,
            SalesWindowStart = domain.SalesWindow?.Start,
            SalesWindowEnd = domain.SalesWindow?.End,
            Sold = domain.Sold,
            Reserved = domain.Reserved,
            CreatedAt = domain.CreatedAt,
            UpdatedAt = domain.UpdatedAt,
        };

    public static TicketType ToDomain(TicketTypeRecord record)
    {
        SalesWindow? salesWindow = null;
        if (record.SalesWindowStart.HasValue && record.SalesWindowEnd.HasValue)
        {
            salesWindow = SalesWindow.Create(record.SalesWindowStart.Value, record.SalesWindowEnd.Value);
        }

        return TicketType.FromPersistence(
            TicketTypeId.From(record.Id),
            TicketName.Create(record.Name),
            Money.Create(record.PriceAmount, record.PriceCurrency),
            Capacity.Create(record.Capacity),
            record.MaxPerOrder,
            salesWindow,
            record.Sold,
            record.Reserved,
            record.CreatedAt,
            record.UpdatedAt);
    }
}
