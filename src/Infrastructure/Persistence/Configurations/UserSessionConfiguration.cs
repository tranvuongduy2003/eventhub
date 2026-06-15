using EventHub.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventHub.Infrastructure.Persistence.Configurations;

internal sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSessionRecord>
{
    public void Configure(EntityTypeBuilder<UserSessionRecord> builder)
    {
        builder.ToTable("user_sessions");

        builder.HasKey(session => session.Id);

        builder.Property(session => session.Id).HasColumnName("id");
        builder.Property(session => session.UserId).HasColumnName("user_id");
        builder.Property(session => session.CreatedAt).HasColumnName("created_at");
        builder.Property(session => session.ExpiresAt).HasColumnName("expires_at");
        builder.Property(session => session.LastSeenAt).HasColumnName("last_seen_at");
        builder.Property(session => session.RevokedAt).HasColumnName("revoked_at");

        builder.HasIndex(session => session.UserId).HasDatabaseName("ix_user_sessions_user");
        builder.HasIndex(session => session.ExpiresAt).HasDatabaseName("ix_user_sessions_expires");

        builder.HasOne(session => session.User)
            .WithMany(user => user.Sessions)
            .HasForeignKey(session => session.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
