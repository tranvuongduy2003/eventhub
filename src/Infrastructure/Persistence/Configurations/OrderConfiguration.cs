using EventHub.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventHub.Infrastructure.Persistence.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<OrderRecord>
{
    public void Configure(EntityTypeBuilder<OrderRecord> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        builder.Property(o => o.EventId).HasColumnName("event_id").IsRequired();
        builder.Property(o => o.ContactName).HasColumnName("contact_name").HasMaxLength(200).IsRequired();
        builder.Property(o => o.ContactEmail).HasColumnName("contact_email").HasMaxLength(300).IsRequired();
        builder.Property(o => o.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        builder.Property(o => o.TotalAmount).HasColumnName("total_amount").HasColumnType("numeric(12,2)").IsRequired();
        builder.Property(o => o.TotalCurrency).HasColumnName("total_currency").HasMaxLength(3).IsRequired();
        builder.Property(o => o.PaymentId).HasColumnName("payment_id");
        builder.Property(o => o.ReservationId).HasColumnName("reservation_id");
        builder.Property(o => o.PlacedAt).HasColumnName("placed_at").IsRequired();
        builder.Property(o => o.ConfirmedAt).HasColumnName("confirmed_at");
        builder.Property(o => o.ExpiresAt).HasColumnName("expires_at");
        builder.Property(o => o.CancelledAt).HasColumnName("cancelled_at");
        builder.Property(o => o.RowVersion).HasColumnName("row_version").IsRowVersion().HasDefaultValue(1L);

        builder.HasIndex(o => o.EventId).HasDatabaseName("ix_orders_event_id");

        builder.HasOne<EventRecord>()
            .WithMany()
            .HasForeignKey(o => o.EventId)
            .HasConstraintName("fk_orders_events_event_id");

        builder.HasMany(o => o.Lines)
            .WithOne()
            .HasForeignKey(l => l.OrderId)
            .HasConstraintName("fk_order_lines_orders_order_id");
    }
}
