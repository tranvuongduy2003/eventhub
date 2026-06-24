using EventHub.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventHub.Infrastructure.Persistence.Configurations;

internal sealed class OrderLineConfiguration : IEntityTypeConfiguration<OrderLineRecord>
{
    public void Configure(EntityTypeBuilder<OrderLineRecord> builder)
    {
        builder.ToTable("order_lines");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        builder.Property(l => l.OrderId).HasColumnName("order_id").IsRequired();
        builder.Property(l => l.TicketTypeId).HasColumnName("ticket_type_id").IsRequired();
        builder.Property(l => l.Quantity).HasColumnName("quantity").IsRequired();
        builder.Property(l => l.UnitPriceAmount).HasColumnName("unit_price_amount").HasColumnType("numeric(12,2)").IsRequired();
        builder.Property(l => l.UnitPriceCurrency).HasColumnName("unit_price_currency").HasMaxLength(3).IsRequired();
        builder.Property(l => l.LineTotalAmount).HasColumnName("line_total_amount").HasColumnType("numeric(12,2)").IsRequired();
        builder.Property(l => l.LineTotalCurrency).HasColumnName("line_total_currency").HasMaxLength(3).IsRequired();

        builder.HasIndex(l => l.OrderId).HasDatabaseName("ix_order_lines_order_id");

        builder.HasOne<TicketTypeRecord>()
            .WithMany()
            .HasForeignKey(l => l.TicketTypeId)
            .HasConstraintName("fk_order_lines_ticket_types_ticket_type_id");
    }
}
