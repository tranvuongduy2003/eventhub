using EventHub.Domain.Abstractions;
using EventHub.Domain.Exceptions;

namespace EventHub.Domain.Events;

public sealed class TicketType : Entity<TicketTypeId>
{
    private TicketType()
    {
    }

    public TicketName Name { get; private set; } = null!;

    public Money Price { get; private set; } = null!;

    public Capacity Capacity { get; private set; } = null!;

    public int? MaxPerOrder { get; private set; }

    public SalesWindow? SalesWindow { get; private set; }

    public int Sold { get; private set; }

    public int Reserved { get; private set; }

    public int Available => Capacity.Value - Sold - Reserved;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static TicketType Create(
        TicketTypeId id,
        TicketName name,
        Money price,
        Capacity capacity,
        int? maxPerOrder,
        SalesWindow? salesWindow,
        DateTimeOffset createdAt)
    {
        if (maxPerOrder is < 1)
        {
            throw new BusinessRuleValidationException(
                "MAX_PER_ORDER_INVALID",
                "Max per order must be at least 1 when set.");
        }

        return new()
        {
            Id = id,
            Name = name,
            Price = price,
            Capacity = capacity,
            MaxPerOrder = maxPerOrder,
            SalesWindow = salesWindow,
            Sold = 0,
            Reserved = 0,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
        };
    }

    public static TicketType FromPersistence(
        TicketTypeId id,
        TicketName name,
        Money price,
        Capacity capacity,
        int? maxPerOrder,
        SalesWindow? salesWindow,
        int sold,
        int reserved,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt) =>
        new()
        {
            Id = id,
            Name = name,
            Price = price,
            Capacity = capacity,
            MaxPerOrder = maxPerOrder,
            SalesWindow = salesWindow,
            Sold = sold,
            Reserved = reserved,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
        };

    public void Reserve(int quantity)
    {
        if (quantity <= 0)
        {
            throw new BusinessRuleValidationException(
                "RESERVATION_QUANTITY_INVALID",
                "Reservation quantity must be at least 1.");
        }

        // INV-10: Reserved + Sold must not exceed Capacity
        if (Reserved + Sold + quantity > Capacity.Value)
        {
            throw new BusinessRuleValidationException(
                "TICKET_TYPE_SOLD_OUT",
                $"Not enough availability for ticket type '{Name.Value}'. Available: {Available}, requested: {quantity}.");
        }

        Reserved += quantity;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void CommitReservation(int quantity)
    {
        if (quantity <= 0)
        {
            throw new BusinessRuleValidationException(
                "RESERVATION_QUANTITY_INVALID",
                "Commit quantity must be at least 1.");
        }

        if (quantity > Reserved)
        {
            throw new BusinessRuleValidationException(
                "INSUFFICIENT_RESERVED",
                $"Cannot commit {quantity} reservation(s) — only {Reserved} reserved.");
        }

        Reserved -= quantity;
        Sold += quantity;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ReleaseReservation(int quantity)
    {
        if (quantity <= 0)
        {
            throw new BusinessRuleValidationException(
                "RESERVATION_QUANTITY_INVALID",
                "Release quantity must be at least 1.");
        }

        if (quantity > Reserved)
        {
            throw new BusinessRuleValidationException(
                "INSUFFICIENT_RESERVED",
                $"Cannot release {quantity} reservation(s) — only {Reserved} reserved.");
        }

        Reserved -= quantity;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ReturnToPool(int quantity)
    {
        if (quantity <= 0)
        {
            throw new BusinessRuleValidationException(
                "RETURN_QUANTITY_INVALID",
                "Return quantity must be at least 1.");
        }

        if (quantity > Sold)
        {
            throw new BusinessRuleValidationException(
                "INSUFFICIENT_SOLD",
                $"Cannot return {quantity} sold ticket(s) — only {Sold} sold.");
        }

        Sold -= quantity;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetCapacity(Capacity newCapacity)
    {
        Capacity = newCapacity;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(TicketName newName, Money newPrice, Capacity newCapacity, int? newMaxPerOrder, SalesWindow? newSalesWindow, DateTimeOffset updatedAt)
    {
        if (newMaxPerOrder is < 1)
        {
            throw new BusinessRuleValidationException(
                "MAX_PER_ORDER_INVALID",
                "Max per order must be at least 1 when set.");
        }

        Name = newName;
        Price = newPrice;
        Capacity = newCapacity;
        MaxPerOrder = newMaxPerOrder;
        SalesWindow = newSalesWindow;
        UpdatedAt = updatedAt;
    }
}
