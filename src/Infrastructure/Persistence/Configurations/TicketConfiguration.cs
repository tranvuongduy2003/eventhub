using EventHub.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventHub.Infrastructure.Persistence.Configurations;

internal sealed class TicketConfiguration : IEntityTypeConfiguration<TicketRecord>
{
    public void Configure(EntityTypeBuilder<TicketRecord> builder)
    {
        builder.ToTable("tickets");

        builder.HasKey(ticket => ticket.Id);

        builder.Property(ticket => ticket.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        builder.Property(ticket => ticket.EventId).HasColumnName("event_id").IsRequired();
        builder.Property(ticket => ticket.OrderId).HasColumnName("order_id").IsRequired();
        builder.Property(ticket => ticket.TicketTypeId).HasColumnName("ticket_type_id").IsRequired();
        builder.Property(ticket => ticket.Code).HasColumnName("code").HasMaxLength(120).IsRequired();
        builder.Property(ticket => ticket.HolderName).HasColumnName("holder_name").HasMaxLength(200).IsRequired();
        builder.Property(ticket => ticket.HolderEmail).HasColumnName("holder_email").HasMaxLength(300).IsRequired();
        builder.Property(ticket => ticket.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        builder.Property(ticket => ticket.IssuedAt).HasColumnName("issued_at").IsRequired();
        builder.Property(ticket => ticket.CheckedInAt).HasColumnName("checked_in_at");
        builder.Property(ticket => ticket.LastDeliveredAt).HasColumnName("last_delivered_at");
        builder.Property(ticket => ticket.RowVersion).HasColumnName("row_version").IsRowVersion().HasDefaultValue(1L);

        builder.HasIndex(ticket => ticket.Code)
            .IsUnique()
            .HasDatabaseName("ux_tickets_code");

        builder.HasIndex(ticket => ticket.OrderId)
            .HasDatabaseName("ix_tickets_order_id");

        builder.HasIndex(ticket => ticket.HolderEmail)
            .HasDatabaseName("ix_tickets_holder_email");

        builder.HasOne<EventRecord>()
            .WithMany()
            .HasForeignKey(ticket => ticket.EventId)
            .HasConstraintName("fk_tickets_events_event_id");

        builder.HasOne<OrderRecord>()
            .WithMany()
            .HasForeignKey(ticket => ticket.OrderId)
            .HasConstraintName("fk_tickets_orders_order_id");

        builder.HasOne<TicketTypeRecord>()
            .WithMany()
            .HasForeignKey(ticket => ticket.TicketTypeId)
            .HasConstraintName("fk_tickets_ticket_types_ticket_type_id");
    }
}
