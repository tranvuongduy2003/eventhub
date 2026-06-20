using EventHub.Application.Abstractions.Persistence;
using EventHub.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Infrastructure.Persistence;

public sealed class ApplicationDatabaseContext(DbContextOptions<ApplicationDatabaseContext> options)
    : DbContext(options), IApplicationDatabaseContext
{
    public const string SchemaName = "app";

    public DbSet<UserRecord> Users => Set<UserRecord>();

    public DbSet<UserSessionRecord> UserSessions => Set<UserSessionRecord>();

    public DbSet<EventUserRoleRecord> EventUserRoles => Set<EventUserRoleRecord>();

    public DbSet<EventInvitationRecord> EventInvitations => Set<EventInvitationRecord>();

    public DbSet<PermissionAuditEntryRecord> PermissionAuditEntries => Set<PermissionAuditEntryRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(global::EventHub.Infrastructure.AssemblyReference.Assembly);
    }
}
