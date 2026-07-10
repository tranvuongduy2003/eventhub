namespace EventHub.Infrastructure.Persistence.Entities;

public sealed class TicketRecord
{
    public int Id { get; set; }

    public int EventId { get; set; }

    public int OrderId { get; set; }

    public int TicketTypeId { get; set; }

    public required string Code { get; set; }

    public required string HolderName { get; set; }

    public required string HolderEmail { get; set; }

    public required string Status { get; set; }

    public DateTimeOffset IssuedAt { get; set; }

    public DateTimeOffset? CheckedInAt { get; set; }

    public DateTimeOffset? LastDeliveredAt { get; set; }

    public long RowVersion { get; set; }
}
