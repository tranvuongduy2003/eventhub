using EventHub.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventHub.Infrastructure.Persistence.Configurations;

internal sealed class CheckInReplayConfiguration : IEntityTypeConfiguration<CheckInReplayRecord>
{
    public void Configure(EntityTypeBuilder<CheckInReplayRecord> builder)
    {
        builder.ToTable("check_in_replays");

        builder.HasKey(replay => replay.Id);

        builder.Property(replay => replay.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        builder.Property(replay => replay.EventId).HasColumnName("event_id").IsRequired();
        builder.Property(replay => replay.ClientScanId)
            .HasColumnName("client_scan_id")
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(replay => replay.CodeFingerprint)
            .HasColumnName("code_fingerprint")
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(replay => replay.ScannedAtUtc)
            .HasColumnName("scanned_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(replay => replay.Accepted).HasColumnName("accepted").IsRequired();
        builder.Property(replay => replay.ResponseStatus)
            .HasColumnName("response_status")
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(replay => replay.RejectionReason)
            .HasColumnName("rejection_reason")
            .HasMaxLength(512);
        builder.Property(replay => replay.TicketId).HasColumnName("ticket_id");
        builder.Property(replay => replay.CheckedInAt)
            .HasColumnName("checked_in_at")
            .HasColumnType("timestamp with time zone");
        builder.Property(replay => replay.ResolvedAt)
            .HasColumnName("resolved_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(replay => new { replay.EventId, replay.ClientScanId })
            .IsUnique()
            .HasDatabaseName("ux_check_in_replays_event_id_client_scan_id");

        builder.HasIndex(replay => replay.TicketId)
            .HasDatabaseName("ix_check_in_replays_ticket_id");

        builder.HasOne<EventRecord>()
            .WithMany()
            .HasForeignKey(replay => replay.EventId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_check_in_replays_events_event_id");

        builder.HasOne<TicketRecord>()
            .WithMany()
            .HasForeignKey(replay => replay.TicketId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_check_in_replays_tickets_ticket_id");
    }
}
