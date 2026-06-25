using EventHub.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventHub.Infrastructure.Persistence.Configurations;

internal sealed class ReservationConfiguration : IEntityTypeConfiguration<ReservationRecord>
{
    public void Configure(EntityTypeBuilder<ReservationRecord> builder)
    {
        builder.ToTable("reservations");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        builder.Property(r => r.EventId).HasColumnName("event_id").IsRequired();
        builder.Property(r => r.TicketTypeId).HasColumnName("ticket_type_id").IsRequired();
        builder.Property(r => r.Quantity).HasColumnName("quantity").IsRequired();
        builder.Property(r => r.OrderId).HasColumnName("order_id").IsRequired();
        builder.Property(r => r.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(r => r.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(r => r.EventId).HasDatabaseName("ix_reservations_event_id");
        builder.HasIndex(r => r.OrderId).HasDatabaseName("ix_reservations_order_id");
        builder.HasIndex(r => r.ExpiresAt).HasDatabaseName("ix_reservations_expires_at");

        builder.HasOne<EventRecord>()
            .WithMany()
            .HasForeignKey(r => r.EventId)
            .HasConstraintName("fk_reservations_events_event_id");

        builder.HasOne<OrderRecord>()
            .WithMany()
            .HasForeignKey(r => r.OrderId)
            .HasConstraintName("fk_reservations_orders_order_id");
    }
}
