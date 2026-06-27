using EventHub.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventHub.Infrastructure.Persistence.Configurations;

internal sealed class TicketTypeConfiguration : IEntityTypeConfiguration<TicketTypeRecord>
{
    public void Configure(EntityTypeBuilder<TicketTypeRecord> builder)
    {
        builder.ToTable("ticket_types");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        builder.Property(t => t.EventId).HasColumnName("event_id").IsRequired();
        builder.Property(t => t.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(t => t.PriceAmount).HasColumnName("price_amount").HasColumnType("numeric(12,2)").IsRequired();
        builder.Property(t => t.PriceCurrency).HasColumnName("price_currency").HasMaxLength(3).IsRequired();
        builder.Property(t => t.Capacity).HasColumnName("capacity").IsRequired();
        builder.Property(t => t.MaxPerOrder).HasColumnName("max_per_order");
        builder.Property(t => t.SalesWindowStart).HasColumnName("sales_window_start");
        builder.Property(t => t.SalesWindowEnd).HasColumnName("sales_window_end");
        builder.Property(t => t.Sold).HasColumnName("sold").HasDefaultValue(0).IsRequired();
        builder.Property(t => t.Reserved).HasColumnName("reserved").HasDefaultValue(0).IsRequired();
        builder.Property(t => t.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasIndex(t => t.EventId).HasDatabaseName("ix_ticket_types_event_id");

        builder.HasOne<EventRecord>()
            .WithMany()
            .HasForeignKey(t => t.EventId)
            .HasConstraintName("fk_ticket_types_events_event_id");
    }
}
