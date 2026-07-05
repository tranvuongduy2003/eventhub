using EventHub.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventHub.Infrastructure.Persistence.Configurations;

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<PaymentRecord>
{
    public void Configure(EntityTypeBuilder<PaymentRecord> builder)
    {
        builder.ToTable("payments");

        builder.HasKey(payment => payment.Id);

        builder.Property(payment => payment.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        builder.Property(payment => payment.OrderId).HasColumnName("order_id").IsRequired();
        builder.Property(payment => payment.Amount).HasColumnName("amount").HasColumnType("numeric(12,2)").IsRequired();
        builder.Property(payment => payment.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        builder.Property(payment => payment.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        builder.Property(payment => payment.ProviderReference).HasColumnName("provider_reference").HasMaxLength(200).IsRequired();
        builder.Property(payment => payment.InitiatedAt).HasColumnName("initiated_at").IsRequired();
        builder.Property(payment => payment.CapturedAt).HasColumnName("captured_at");
        builder.Property(payment => payment.FailedAt).HasColumnName("failed_at");
        builder.Property(payment => payment.RefundedAt).HasColumnName("refunded_at");
        builder.Property(payment => payment.RowVersion).HasColumnName("row_version").IsRowVersion().HasDefaultValue(1L);

        builder.HasIndex(payment => payment.OrderId).HasDatabaseName("ix_payments_order_id");
        builder.HasIndex(payment => payment.ProviderReference)
            .IsUnique()
            .HasDatabaseName("ux_payments_provider_reference");

        builder.HasOne<OrderRecord>()
            .WithMany()
            .HasForeignKey(payment => payment.OrderId)
            .HasConstraintName("fk_payments_orders_order_id");
    }
}
