using EventHub.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventHub.Infrastructure.Persistence.Configurations;

internal sealed class EventInvitationConfiguration : IEntityTypeConfiguration<EventInvitationRecord>
{
    public void Configure(EntityTypeBuilder<EventInvitationRecord> builder)
    {
        builder.ToTable("event_invitation");

        builder.HasKey(invitation => invitation.Id);

        builder.Property(invitation => invitation.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(invitation => invitation.EventId).HasColumnName("event_id");
        builder.Property(invitation => invitation.Email).HasColumnName("email").HasMaxLength(254).IsRequired();
        builder.Property(invitation => invitation.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(invitation => invitation.TokenHash).HasColumnName("token_hash").IsRequired();
        builder.Property(invitation => invitation.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(invitation => invitation.InviterId).HasColumnName("inviter_id");
        builder.Property(invitation => invitation.CreatedAt).HasColumnName("created_at");
        builder.Property(invitation => invitation.ExpiresAt).HasColumnName("expires_at");
        builder.Property(invitation => invitation.AcceptedAt).HasColumnName("accepted_at");
        builder.Property(invitation => invitation.RevokedAt).HasColumnName("revoked_at");

        builder.HasIndex(invitation => invitation.EventId)
            .HasDatabaseName("ix_event_invitation_event_id");

        builder.HasIndex(invitation => invitation.TokenHash)
            .IsUnique()
            .HasDatabaseName("ux_event_invitation_token_hash");

        builder.HasIndex(invitation => new { invitation.EventId, invitation.Email, invitation.Status })
            .HasDatabaseName("ix_event_invitation_event_id_email_status");

        // Partial unique index: at most one Pending invitation per (event_id, email)
        builder.HasIndex(invitation => new { invitation.EventId, invitation.Email })
            .IsUnique()
            .HasDatabaseName("ux_event_invitation_pending_email")
            .HasFilter("status = 'Pending'");

        builder.HasOne(invitation => invitation.Inviter)
            .WithMany()
            .HasForeignKey(invitation => invitation.InviterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
